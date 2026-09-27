using Api.Domain;
using Api.DTOs;
using Api.Persistence;
using Api.Solver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly UniSchedulingDbContext _context;
    private readonly ScheduleSolver _solver;

    public SchedulesController(UniSchedulingDbContext context, ScheduleSolver solver)
    {
        _context = context;
        _solver = solver;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateScheduleResponse>> Generate(
        [FromBody] GenerateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new GenerateScheduleResponse
            {
                IsSuccess = false,
                ErrorMessage = "Request cannot be null."
            });
        }

        var offeringIds = request.SelectedCourseOfferingIds?.Distinct().ToList() ?? [];

        if (offeringIds.Count == 0)
        {
            var emptyResult = _solver.GenerateSchedules(new SchedulingInputSnapshot(
                new StudentGroupConstraint(request.AcademicLevelNumber, request.AssignedPrimaryGroupId),
                [],
                []
            ));
            return Ok(MapToResponse(emptyResult));
        }

        // a) Read requested CourseOffering joined with Course and AcademicLevel
        var offeringDetails = await (
            from co in _context.CourseOfferings
            where offeringIds.Contains(co.Id)
            join c in _context.Courses on co.CourseId equals c.Id
            join al in _context.AcademicLevels on c.AcademicLevelId equals al.Id
            select new
            {
                OfferingId = co.Id,
                CourseId = c.Id,
                CourseCode = c.Code,
                CourseName = c.Name,
                AcademicLevelNumber = al.LevelNumber
            }
        ).ToListAsync(cancellationToken);

        // Fetch associated activities for the requested offerings
        var activities = await _context.Activities
            .Where(a => offeringIds.Contains(a.CourseOfferingId))
            .ToListAsync(cancellationToken);

        var activityIds = activities.Select(a => a.Id).Distinct().ToList();

        // Fetch associated activity groups for these activities
        var activityGroups = await _context.ActivityGroups
            .Where(ag => activityIds.Contains(ag.ActivityId))
            .ToListAsync(cancellationToken);

        var groupIds = activityGroups.Select(ag => ag.Id).Distinct().ToList();

        // b) Fetch associated Meeting records by ActivityGroupId
        var meetings = await _context.Meetings
            .Where(m => groupIds.Contains(m.ActivityGroupId))
            .ToListAsync(cancellationToken);

        var meetingsByGroupId = meetings
            .GroupBy(m => m.ActivityGroupId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Meeting>)g.ToList());

        var activitiesById = activities.ToDictionary(a => a.Id);
        var offeringDetailsById = offeringDetails.ToDictionary(o => o.OfferingId);

        // c) Map database results into IReadOnlyList<SelectedCourseRequirement> and IReadOnlyList<ActivityGroupOption>
        var availableOptions = new List<ActivityGroupOption>();
        foreach (var group in activityGroups)
        {
            if (!activitiesById.TryGetValue(group.ActivityId, out var activity))
            {
                continue;
            }

            if (!offeringDetailsById.TryGetValue(activity.CourseOfferingId, out var offering))
            {
                continue;
            }

            meetingsByGroupId.TryGetValue(group.Id, out var groupMeetings);
            groupMeetings ??= [];

            var option = ActivityGroupOption.FromDomain(
                group: group,
                courseOfferingId: offering.OfferingId,
                activityType: activity.Type,
                meetings: groupMeetings,
                courseCode: offering.CourseCode,
                academicLevelNumber: offering.AcademicLevelNumber
            );

            availableOptions.Add(option);
        }

        var selectedCourses = new List<SelectedCourseRequirement>();
        foreach (var offering in offeringDetails)
        {
            var offeringActivities = activities
                .Where(a => a.CourseOfferingId == offering.OfferingId)
                .ToList();

            var actReqs = offeringActivities
                .Select(a => new ActivityRequirement(a.Type, a.Id))
                .ToList();

            selectedCourses.Add(new SelectedCourseRequirement(
                courseOfferingId: offering.OfferingId,
                courseCode: offering.CourseCode,
                activityRequirements: actReqs,
                academicLevelNumber: offering.AcademicLevelNumber
            ));
        }

        // d) Construct StudentGroupConstraint using AcademicLevelNumber and AssignedPrimaryGroupId
        var studentConstraint = new StudentGroupConstraint(
            request.AcademicLevelNumber,
            request.AssignedPrimaryGroupId
        );

        // e) Build SchedulingInputSnapshot and execute solver
        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: studentConstraint,
            selectedCourses: selectedCourses,
            availableOptions: availableOptions
        );

        var result = _solver.GenerateSchedules(snapshot);

        // f) Map SchedulingResult into GenerateScheduleResponse and return Ok(response)
        var response = MapToResponse(result);
        return Ok(response);
    }

    private static GenerateScheduleResponse MapToResponse(SchedulingResult result)
    {
        return new GenerateScheduleResponse
        {
            IsSuccess = result.IsSuccess,
            TotalCombinationsEvaluated = result.TotalCombinationsEvaluated,
            ErrorMessage = result.ErrorMessage,
            Schedules = result.ValidSchedules.Select(schedule => new ScheduleDto
            {
                ScheduleId = schedule.ScheduleId,
                SelectedGroups = schedule.SelectedGroups.Select(group => new SelectedGroupDto
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    CourseCode = group.CourseCode,
                    ActivityType = group.ActivityType.ToString(),
                    Meetings = group.Meetings.Select(m => new MeetingDto
                    {
                        DayOfWeek = m.DayOfWeek,
                        StartTime = m.StartTime,
                        EndTime = m.EndTime,
                        Room = m.Room
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }
}
