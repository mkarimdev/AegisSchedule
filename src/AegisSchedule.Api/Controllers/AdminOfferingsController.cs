using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using AegisSchedule.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisSchedule.Api.Controllers;

[ApiController]
[ApiKeyAuth]
[Route("api/admin/offerings")]
public class AdminOfferingsController : ControllerBase
{
    private readonly UniSchedulingDbContext _context;

    public AdminOfferingsController(UniSchedulingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourseOfferingDto>>> GetOfferings(
        [FromQuery] Guid? termId,
        [FromQuery] int? academicLevel,
        [FromQuery] Guid? courseId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = from co in _context.CourseOfferings
                        join c in _context.Courses on co.CourseId equals c.Id
                        join al in _context.AcademicLevels on c.AcademicLevelId equals al.Id
                        join t in _context.Terms on co.TermId equals t.Id
                        select new
                        {
                            OfferingId = co.Id,
                            CourseId = c.Id,
                            CourseCode = c.Code,
                            CourseName = c.Name,
                            AcademicLevelId = al.Id,
                            AcademicLevelNumber = al.LevelNumber,
                            TermId = t.Id,
                            TermSemester = t.Semester,
                            TermYear = t.Year
                        };

        if (termId.HasValue && termId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.TermId == termId.Value);
        }

        if (academicLevel.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.AcademicLevelNumber == academicLevel.Value);
        }

        if (courseId.HasValue && courseId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.CourseId == courseId.Value);
        }

        var rawOfferings = await baseQuery
            .OrderBy(x => x.AcademicLevelNumber)
            .ThenBy(x => x.CourseCode)
            .ToListAsync(cancellationToken);

        if (rawOfferings.Count == 0)
        {
            return Ok(new List<CourseOfferingDto>());
        }

        var offeringIds = rawOfferings.Select(x => x.OfferingId).ToList();

        var activities = await _context.Activities
            .Where(a => offeringIds.Contains(a.CourseOfferingId))
            .ToListAsync(cancellationToken);

        var activityIds = activities.Select(a => a.Id).Distinct().ToList();

        var groups = await _context.ActivityGroups
            .Where(ag => activityIds.Contains(ag.ActivityId))
            .OrderBy(ag => ag.Name)
            .ToListAsync(cancellationToken);

        var activitiesByOfferingId = activities
            .GroupBy(a => a.CourseOfferingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groupsByActivityId = groups
            .GroupBy(g => g.ActivityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = rawOfferings.Select(raw =>
        {
            activitiesByOfferingId.TryGetValue(raw.OfferingId, out var offeringActs);
            offeringActs ??= [];

            var offeringGroups = new List<CatalogActivityGroupDto>();
            foreach (var act in offeringActs)
            {
                groupsByActivityId.TryGetValue(act.Id, out var actGroups);
                actGroups ??= [];

                foreach (var grp in actGroups)
                {
                    offeringGroups.Add(new CatalogActivityGroupDto
                    {
                        Id = grp.Id,
                        ActivityId = act.Id,
                        ActivityType = act.Type.ToString(),
                        Name = grp.Name
                    });
                }
            }

            return new CourseOfferingDto
            {
                Id = raw.OfferingId,
                CourseId = raw.CourseId,
                CourseCode = raw.CourseCode,
                CourseName = raw.CourseName,
                AcademicLevelId = raw.AcademicLevelId,
                AcademicLevelNumber = raw.AcademicLevelNumber,
                TermId = raw.TermId,
                TermName = $"{raw.TermSemester} {raw.TermYear}",
                ActivityTypes = offeringActs.Select(a => a.Type.ToString()).Distinct().ToList(),
                Groups = offeringGroups
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminOfferingDetailDto>> GetOfferingById(Guid id, CancellationToken cancellationToken = default)
    {
        var rawOffering = await (from co in _context.CourseOfferings
                                 where co.Id == id
                                 join c in _context.Courses on co.CourseId equals c.Id
                                 join al in _context.AcademicLevels on c.AcademicLevelId equals al.Id
                                 join t in _context.Terms on co.TermId equals t.Id
                                 select new
                                 {
                                     OfferingId = co.Id,
                                     CourseId = c.Id,
                                     CourseCode = c.Code,
                                     CourseName = c.Name,
                                     AcademicLevelNumber = al.LevelNumber,
                                     TermId = t.Id,
                                     TermSemester = t.Semester,
                                     TermYear = t.Year
                                 }).FirstOrDefaultAsync(cancellationToken);

        if (rawOffering == null)
        {
            return NotFound(new { error = $"Course offering with ID '{id}' was not found." });
        }

        var activities = await _context.Activities
            .Where(a => a.CourseOfferingId == id)
            .OrderBy(a => a.Type)
            .ToListAsync(cancellationToken);

        var activityIds = activities.Select(a => a.Id).ToList();

        var groups = await _context.ActivityGroups
            .Where(ag => activityIds.Contains(ag.ActivityId))
            .OrderBy(ag => ag.Name)
            .ToListAsync(cancellationToken);

        var groupIds = groups.Select(g => g.Id).ToList();

        var meetings = await _context.Meetings
            .Where(m => groupIds.Contains(m.ActivityGroupId))
            .OrderBy(m => m.DayOfWeek)
            .ThenBy(m => m.StartTime)
            .ToListAsync(cancellationToken);

        var meetingsByGroupId = meetings
            .GroupBy(m => m.ActivityGroupId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groupsByActivityId = groups
            .GroupBy(g => g.ActivityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var activityDtos = activities.Select(act =>
        {
            groupsByActivityId.TryGetValue(act.Id, out var actGroups);
            actGroups ??= [];

            var groupDtos = actGroups.Select(grp =>
            {
                meetingsByGroupId.TryGetValue(grp.Id, out var grpMeetings);
                grpMeetings ??= [];

                return new AdminActivityGroupDetailDto
                {
                    Id = grp.Id,
                    Name = grp.Name,
                    Meetings = grpMeetings.Select(m => new AdminMeetingDetailDto
                    {
                        Id = m.Id,
                        DayOfWeek = m.DayOfWeek,
                        StartTime = m.StartTime.ToString("HH:mm"),
                        EndTime = m.EndTime.ToString("HH:mm"),
                        Room = m.Room
                    }).ToList()
                };
            }).ToList();

            return new AdminActivityDetailDto
            {
                Id = act.Id,
                Type = act.Type.ToString(),
                Groups = groupDtos
            };
        }).ToList();

        var detailDto = new AdminOfferingDetailDto
        {
            Id = rawOffering.OfferingId,
            CourseId = rawOffering.CourseId,
            CourseCode = rawOffering.CourseCode,
            CourseName = rawOffering.CourseName,
            AcademicLevelNumber = rawOffering.AcademicLevelNumber,
            TermId = rawOffering.TermId,
            TermName = $"{rawOffering.TermSemester} {rawOffering.TermYear}",
            Activities = activityDtos
        };

        return Ok(detailDto);
    }

    [HttpPost]
    public async Task<ActionResult<AdminOfferingDetailDto>> CreateOffering(
        [FromBody] CreateOfferingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        var course = await _context.Courses.FindAsync([request.CourseId], cancellationToken);
        if (course == null)
        {
            return BadRequest(new { error = $"Course with ID '{request.CourseId}' does not exist." });
        }

        var term = await _context.Terms.FindAsync([request.TermId], cancellationToken);
        if (term == null)
        {
            return BadRequest(new { error = $"Term with ID '{request.TermId}' does not exist." });
        }

        var isDuplicate = await _context.CourseOfferings.AnyAsync(
            co => co.CourseId == request.CourseId && co.TermId == request.TermId,
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = "This course is already offered in the specified term." });
        }

        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = request.CourseId,
            TermId = request.TermId,
            UniversityId = course.UniversityId
        };

        await _context.CourseOfferings.AddAsync(offering, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetOfferingById(offering.Id, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOffering(Guid id, CancellationToken cancellationToken = default)
    {
        var offering = await _context.CourseOfferings.FindAsync([id], cancellationToken);
        if (offering == null)
        {
            return NotFound(new { error = $"Course offering with ID '{id}' was not found." });
        }

        var activities = await _context.Activities
            .Where(a => a.CourseOfferingId == id)
            .ToListAsync(cancellationToken);

        var activityIds = activities.Select(a => a.Id).ToList();

        var groups = await _context.ActivityGroups
            .Where(ag => activityIds.Contains(ag.ActivityId))
            .ToListAsync(cancellationToken);

        var groupIds = groups.Select(g => g.Id).ToList();

        var meetings = await _context.Meetings
            .Where(m => groupIds.Contains(m.ActivityGroupId))
            .ToListAsync(cancellationToken);

        _context.Meetings.RemoveRange(meetings);
        _context.ActivityGroups.RemoveRange(groups);
        _context.Activities.RemoveRange(activities);
        _context.CourseOfferings.Remove(offering);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
