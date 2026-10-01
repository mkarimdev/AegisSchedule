using System.Data;
using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Activity = AegisSchedule.Api.Domain.Activity;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace AegisSchedule.Api.Integrations;

public class SisImportService : ISisImportService
{
    private readonly UniSchedulingDbContext _context;
    private readonly ILogger<SisImportService> _logger;

    public SisImportService(UniSchedulingDbContext context, ILogger<SisImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SisBatchSyncResult> SyncBatchAsync(SisBatchSyncRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // -------------------------------------------------------------
        // Phase 1: Pure In-Memory Validation (Fail Fast)
        // -------------------------------------------------------------
        var validation = SisBatchValidator.Validate(request);
        if (!validation.IsValid)
        {
            stopwatch.Stop();
            return new SisBatchSyncResult
            {
                Success = false,
                Errors = validation.Errors,
                Warnings = validation.Warnings,
                Metrics = new SisSyncMetrics { ElapsedMilliseconds = stopwatch.ElapsedMilliseconds }
            };
        }

        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        }

        try
        {
            // -------------------------------------------------------------
            // Resolve University
            // -------------------------------------------------------------
            Guid universityId;
            if (request.UniversityId.HasValue && request.UniversityId.Value != Guid.Empty)
            {
                var uniExists = await _context.Universities.AnyAsync(u => u.Id == request.UniversityId.Value, cancellationToken);
                if (!uniExists)
                {
                    stopwatch.Stop();
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return new SisBatchSyncResult
                    {
                        Success = false,
                        Errors = [
                            new SisSyncDiagnostic
                            {
                                Code = "UNIVERSITY_NOT_FOUND",
                                EntityPath = "UniversityId",
                                Message = $"University with ID '{request.UniversityId.Value}' does not exist."
                            }
                        ],
                        Metrics = new SisSyncMetrics { ElapsedMilliseconds = stopwatch.ElapsedMilliseconds }
                    };
                }
                universityId = request.UniversityId.Value;
            }
            else
            {
                var defaultUni = await _context.Universities.FirstOrDefaultAsync(cancellationToken);
                if (defaultUni == null)
                {
                    stopwatch.Stop();
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return new SisBatchSyncResult
                    {
                        Success = false,
                        Errors = [
                            new SisSyncDiagnostic
                            {
                                Code = "NO_UNIVERSITY_CONFIGURED",
                                EntityPath = "UniversityId",
                                Message = "No university found in the system to associate with the imported catalog."
                            }
                        ],
                        Metrics = new SisSyncMetrics { ElapsedMilliseconds = stopwatch.ElapsedMilliseconds }
                    };
                }
                universityId = defaultUni.Id;
            }

            // -------------------------------------------------------------
            // Resolve Academic Levels in Bulk
            // -------------------------------------------------------------
            var academicLevels = await _context.AcademicLevels
                .ToDictionaryAsync(al => al.LevelNumber, cancellationToken);

            var missingLevels = request.Courses
                .Where(c => !academicLevels.ContainsKey(c.AcademicLevelNumber))
                .Select(c => c.AcademicLevelNumber)
                .Distinct()
                .ToList();

            if (missingLevels.Count > 0)
            {
                stopwatch.Stop();
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                return new SisBatchSyncResult
                {
                    Success = false,
                    Errors = missingLevels.Select(lvl => new SisSyncDiagnostic
                    {
                        Code = "ACADEMIC_LEVEL_NOT_FOUND",
                        EntityPath = "Courses[].AcademicLevelNumber",
                        Message = $"Academic level with LevelNumber '{lvl}' was not found in the database."
                    }).ToList(),
                    Metrics = new SisSyncMetrics { ElapsedMilliseconds = stopwatch.ElapsedMilliseconds }
                };
            }

            // -------------------------------------------------------------
            // Resolve Term
            // -------------------------------------------------------------
            var term = await _context.Terms.FirstOrDefaultAsync(
                t => t.Semester == request.Term.Semester && t.Year == request.Term.Year,
                cancellationToken);

            if (term == null)
            {
                term = new Term
                {
                    Id = Guid.NewGuid(),
                    Semester = request.Term.Semester,
                    Year = request.Term.Year,
                    IsCurrent = request.Term.SetAsCurrent ?? false
                };
                await _context.Terms.AddAsync(term, cancellationToken);
            }
            else if (request.Term.SetAsCurrent.HasValue)
            {
                term.IsCurrent = request.Term.SetAsCurrent.Value;
            }

            if (term.IsCurrent)
            {
                var otherCurrentTerms = await _context.Terms
                    .Where(t => t.Id != term.Id && t.IsCurrent)
                    .ToListAsync(cancellationToken);
                foreach (var t in otherCurrentTerms)
                {
                    t.IsCurrent = false;
                }
            }

            // -------------------------------------------------------------
            // Bulk Pre-Load Existing Entities to Prevent N+1 Loops
            // -------------------------------------------------------------
            var inboundCodes = request.Courses
                .Select(c => c.Code.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            var existingCourses = await _context.Courses
                .Where(c => c.UniversityId == universityId)
                .ToListAsync(cancellationToken);

            var existingCoursesByCode = existingCourses
                .ToDictionary(c => c.Code.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

            var existingOfferings = await _context.CourseOfferings
                .Where(co => co.TermId == term.Id)
                .ToListAsync(cancellationToken);

            var existingOfferingsByCourseId = existingOfferings
                .ToDictionary(co => co.CourseId);

            var existingOfferingIds = existingOfferings.Select(o => o.Id).ToList();

            var existingActivities = await _context.Activities
                .Where(a => existingOfferingIds.Contains(a.CourseOfferingId))
                .ToListAsync(cancellationToken);

            var existingActivitiesByOfferingId = existingActivities
                .GroupBy(a => a.CourseOfferingId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var existingActivityIds = existingActivities.Select(a => a.Id).ToList();

            var existingGroups = await _context.ActivityGroups
                .Where(ag => existingActivityIds.Contains(ag.ActivityId))
                .ToListAsync(cancellationToken);

            var existingGroupsByActivityId = existingGroups
                .GroupBy(ag => ag.ActivityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var existingGroupIds = existingGroups.Select(g => g.Id).ToList();

            var existingMeetings = await _context.Meetings
                .Where(m => existingGroupIds.Contains(m.ActivityGroupId))
                .ToListAsync(cancellationToken);

            var existingMeetingsByGroupId = existingMeetings
                .GroupBy(m => m.ActivityGroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var metrics = new SisSyncMetrics();

            // -------------------------------------------------------------
            // Handle FullTermReconcile: purge offerings absent from payload
            // -------------------------------------------------------------
            if (request.Mode == SisSyncMode.FullTermReconcile)
            {
                var inboundCodeSet = new HashSet<string>(inboundCodes, StringComparer.OrdinalIgnoreCase);
                foreach (var oldOffering in existingOfferings.ToList())
                {
                    var matchCourse = existingCourses.FirstOrDefault(c => c.Id == oldOffering.CourseId);
                    if (matchCourse != null && !inboundCodeSet.Contains(matchCourse.Code))
                    {
                        RemoveOfferingCascade(oldOffering, existingActivitiesByOfferingId, existingGroupsByActivityId, existingMeetingsByGroupId, metrics);
                        existingOfferingsByCourseId.Remove(oldOffering.CourseId);
                    }
                }
            }

            // -------------------------------------------------------------
            // Reconcile Courses, Offerings, Activities, Groups, Meetings
            // -------------------------------------------------------------
            foreach (var courseDto in request.Courses)
            {
                var codeKey = courseDto.Code.Trim().ToUpperInvariant();
                var level = academicLevels[courseDto.AcademicLevelNumber];

                Course course;
                if (existingCoursesByCode.TryGetValue(codeKey, out var existingCourse))
                {
                    bool modified = false;
                    if (existingCourse.Name != courseDto.Name.Trim())
                    {
                        existingCourse.Name = courseDto.Name.Trim();
                        modified = true;
                    }
                    if (existingCourse.AcademicLevelId != level.Id)
                    {
                        existingCourse.AcademicLevelId = level.Id;
                        modified = true;
                    }
                    if (modified)
                    {
                        metrics.CoursesUpdated++;
                    }
                    course = existingCourse;
                }
                else
                {
                    course = new Course
                    {
                        Id = Guid.NewGuid(),
                        UniversityId = universityId,
                        AcademicLevelId = level.Id,
                        Code = courseDto.Code.Trim(),
                        Name = courseDto.Name.Trim()
                    };
                    await _context.Courses.AddAsync(course, cancellationToken);
                    existingCoursesByCode[codeKey] = course;
                    metrics.CoursesCreated++;
                }

                CourseOffering offering;
                if (existingOfferingsByCourseId.TryGetValue(course.Id, out var existingOffering))
                {
                    offering = existingOffering;
                }
                else
                {
                    offering = new CourseOffering
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        TermId = term.Id,
                        UniversityId = universityId
                    };
                    await _context.CourseOfferings.AddAsync(offering, cancellationToken);
                    existingOfferingsByCourseId[course.Id] = offering;
                    metrics.OfferingsCreated++;
                }

                if (request.Mode == SisSyncMode.ReplaceOfferings)
                {
                    if (existingActivitiesByOfferingId.TryGetValue(offering.Id, out var oldActs))
                    {
                        foreach (var oldAct in oldActs)
                        {
                            if (existingGroupsByActivityId.TryGetValue(oldAct.Id, out var oldGrps))
                            {
                                foreach (var oldGrp in oldGrps)
                                {
                                    if (existingMeetingsByGroupId.TryGetValue(oldGrp.Id, out var oldMeets))
                                    {
                                        _context.Meetings.RemoveRange(oldMeets);
                                        metrics.EntitiesRemoved += oldMeets.Count;
                                        existingMeetingsByGroupId.Remove(oldGrp.Id);
                                    }
                                    _context.ActivityGroups.Remove(oldGrp);
                                    metrics.EntitiesRemoved++;
                                }
                                existingGroupsByActivityId.Remove(oldAct.Id);
                            }
                            _context.Activities.Remove(oldAct);
                            metrics.EntitiesRemoved++;
                        }
                        existingActivitiesByOfferingId.Remove(offering.Id);
                    }
                }

                existingActivitiesByOfferingId.TryGetValue(offering.Id, out var currentActs);
                currentActs ??= [];

                foreach (var actDto in courseDto.Activities)
                {
                    var act = currentActs.FirstOrDefault(a => a.Type == actDto.Type);
                    if (act == null)
                    {
                        act = new Activity
                        {
                            Id = Guid.NewGuid(),
                            CourseOfferingId = offering.Id,
                            Type = actDto.Type
                        };
                        await _context.Activities.AddAsync(act, cancellationToken);
                        currentActs.Add(act);
                        existingActivitiesByOfferingId[offering.Id] = currentActs;
                        metrics.ActivitiesCreated++;
                    }

                    existingGroupsByActivityId.TryGetValue(act.Id, out var currentGrps);
                    currentGrps ??= [];

                    foreach (var grpDto in actDto.Groups)
                    {
                        var trimmedGrpName = grpDto.Name.Trim();
                        var grp = currentGrps.FirstOrDefault(g => string.Equals(g.Name, trimmedGrpName, StringComparison.OrdinalIgnoreCase));
                        if (grp == null)
                        {
                            grp = new ActivityGroup
                            {
                                Id = Guid.NewGuid(),
                                ActivityId = act.Id,
                                Name = trimmedGrpName
                            };
                            await _context.ActivityGroups.AddAsync(grp, cancellationToken);
                            currentGrps.Add(grp);
                            existingGroupsByActivityId[act.Id] = currentGrps;
                            metrics.GroupsCreated++;
                        }

                        existingMeetingsByGroupId.TryGetValue(grp.Id, out var currentMeets);
                        currentMeets ??= [];

                        bool meetingsEqual = currentMeets.Count == grpDto.Meetings.Count &&
                            grpDto.Meetings.All(dto => currentMeets.Any(m =>
                                m.DayOfWeek == dto.DayOfWeek &&
                                m.StartTime == dto.StartTime &&
                                m.EndTime == dto.EndTime &&
                                string.Equals(m.Room ?? "", dto.Room?.Trim() ?? "", StringComparison.OrdinalIgnoreCase)));

                        if (!meetingsEqual)
                        {
                            if (currentMeets.Count > 0)
                            {
                                _context.Meetings.RemoveRange(currentMeets);
                                metrics.EntitiesRemoved += currentMeets.Count;
                                currentMeets.Clear();
                            }

                            foreach (var meetDto in grpDto.Meetings)
                            {
                                var meet = new Meeting
                                {
                                    Id = Guid.NewGuid(),
                                    ActivityGroupId = grp.Id,
                                    DayOfWeek = meetDto.DayOfWeek,
                                    StartTime = meetDto.StartTime,
                                    EndTime = meetDto.EndTime,
                                    Room = meetDto.Room?.Trim()
                                };
                                await _context.Meetings.AddAsync(meet, cancellationToken);
                                currentMeets.Add(meet);
                                metrics.MeetingsCreated++;
                            }
                            existingMeetingsByGroupId[grp.Id] = currentMeets;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            stopwatch.Stop();
            metrics.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            return new SisBatchSyncResult
            {
                Success = true,
                TermId = term.Id,
                TermName = $"{term.Semester} {term.Year}",
                Metrics = metrics,
                Warnings = validation.Warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SIS batch sync failed for term {Semester} {Year}.", request.Term.Semester, request.Term.Year);
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            stopwatch.Stop();
            return new SisBatchSyncResult
            {
                Success = false,
                Errors = [
                    new SisSyncDiagnostic
                    {
                        Code = "TRANSACTION_EXECUTION_FAILED",
                        EntityPath = "$",
                        Message = ex.Message
                    }
                ],
                Metrics = new SisSyncMetrics { ElapsedMilliseconds = stopwatch.ElapsedMilliseconds }
            };
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private void RemoveOfferingCascade(
        CourseOffering offering,
        Dictionary<Guid, List<Activity>> activitiesByOffering,
        Dictionary<Guid, List<ActivityGroup>> groupsByActivity,
        Dictionary<Guid, List<Meeting>> meetingsByGroup,
        SisSyncMetrics metrics)
    {
        if (activitiesByOffering.TryGetValue(offering.Id, out var acts))
        {
            foreach (var act in acts)
            {
                if (groupsByActivity.TryGetValue(act.Id, out var grps))
                {
                    foreach (var grp in grps)
                    {
                        if (meetingsByGroup.TryGetValue(grp.Id, out var meets))
                        {
                            _context.Meetings.RemoveRange(meets);
                            metrics.EntitiesRemoved += meets.Count;
                            meetingsByGroup.Remove(grp.Id);
                        }
                        _context.ActivityGroups.Remove(grp);
                        metrics.EntitiesRemoved++;
                    }
                    groupsByActivity.Remove(act.Id);
                }
                _context.Activities.Remove(act);
                metrics.EntitiesRemoved++;
            }
            activitiesByOffering.Remove(offering.Id);
        }
        _context.CourseOfferings.Remove(offering);
        metrics.EntitiesRemoved++;
    }
}
