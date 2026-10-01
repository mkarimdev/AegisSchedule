using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using AegisSchedule.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisSchedule.Api.Controllers;

[ApiController]
[ApiKeyAuth]
[Route("api/admin")]
public class AdminScheduleSlotsController : ControllerBase
{
    private readonly UniSchedulingDbContext _context;

    public AdminScheduleSlotsController(UniSchedulingDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------
    // Activities
    // -------------------------------------------------------------

    [HttpPost("offerings/{offeringId:guid}/activities")]
    public async Task<IActionResult> CreateActivity(
        Guid offeringId,
        [FromBody] CreateActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        var offeringExists = await _context.CourseOfferings.AnyAsync(co => co.Id == offeringId, cancellationToken);
        if (!offeringExists)
        {
            return NotFound(new { error = $"Course offering with ID '{offeringId}' was not found." });
        }

        var isDuplicate = await _context.Activities.AnyAsync(
            a => a.CourseOfferingId == offeringId && a.Type == request.Type,
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = $"Activity of type '{request.Type}' already exists for this course offering." });
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            CourseOfferingId = offeringId,
            Type = request.Type
        };

        await _context.Activities.AddAsync(activity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new AdminActivityResponse
        {
            Id = activity.Id,
            CourseOfferingId = activity.CourseOfferingId,
            Type = activity.Type.ToString()
        });
    }

    [HttpDelete("activities/{id:guid}")]
    public async Task<IActionResult> DeleteActivity(Guid id, CancellationToken cancellationToken = default)
    {
        var activity = await _context.Activities.FindAsync([id], cancellationToken);
        if (activity == null)
        {
            return NotFound(new { error = $"Activity with ID '{id}' was not found." });
        }

        var groups = await _context.ActivityGroups
            .Where(ag => ag.ActivityId == id)
            .ToListAsync(cancellationToken);

        var groupIds = groups.Select(g => g.Id).ToList();

        var meetings = await _context.Meetings
            .Where(m => groupIds.Contains(m.ActivityGroupId))
            .ToListAsync(cancellationToken);

        _context.Meetings.RemoveRange(meetings);
        _context.ActivityGroups.RemoveRange(groups);
        _context.Activities.Remove(activity);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // -------------------------------------------------------------
    // Activity Groups (Sections)
    // -------------------------------------------------------------

    [HttpPost("activities/{activityId:guid}/groups")]
    public async Task<IActionResult> CreateActivityGroup(
        Guid activityId,
        [FromBody] CreateActivityGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Group name is required." });
        }

        var activityExists = await _context.Activities.AnyAsync(a => a.Id == activityId, cancellationToken);
        if (!activityExists)
        {
            return NotFound(new { error = $"Activity with ID '{activityId}' was not found." });
        }

        var normalizedName = request.Name.Trim();
        var isDuplicate = await _context.ActivityGroups.AnyAsync(
            ag => ag.ActivityId == activityId && ag.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = $"Activity group with name '{normalizedName}' already exists under this activity." });
        }

        var group = new ActivityGroup
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            Name = normalizedName
        };

        await _context.ActivityGroups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new AdminActivityGroupResponse
        {
            Id = group.Id,
            ActivityId = group.ActivityId,
            Name = group.Name
        });
    }

    [HttpPut("groups/{id:guid}")]
    public async Task<IActionResult> UpdateActivityGroup(
        Guid id,
        [FromBody] UpdateActivityGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Group name is required." });
        }

        var group = await _context.ActivityGroups.FindAsync([id], cancellationToken);
        if (group == null)
        {
            return NotFound(new { error = $"Activity group with ID '{id}' was not found." });
        }

        var normalizedName = request.Name.Trim();
        var isDuplicate = await _context.ActivityGroups.AnyAsync(
            ag => ag.Id != id && ag.ActivityId == group.ActivityId && ag.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = $"Activity group with name '{normalizedName}' already exists under this activity." });
        }

        group.Name = normalizedName;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new AdminActivityGroupResponse
        {
            Id = group.Id,
            ActivityId = group.ActivityId,
            Name = group.Name
        });
    }

    [HttpDelete("groups/{id:guid}")]
    public async Task<IActionResult> DeleteActivityGroup(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _context.ActivityGroups.FindAsync([id], cancellationToken);
        if (group == null)
        {
            return NotFound(new { error = $"Activity group with ID '{id}' was not found." });
        }

        var meetings = await _context.Meetings
            .Where(m => m.ActivityGroupId == id)
            .ToListAsync(cancellationToken);

        _context.Meetings.RemoveRange(meetings);
        _context.ActivityGroups.Remove(group);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // -------------------------------------------------------------
    // Meetings
    // -------------------------------------------------------------

    [HttpPost("groups/{groupId:guid}/meetings")]
    public async Task<IActionResult> CreateMeeting(
        Guid groupId,
        [FromBody] CreateMeetingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        if (request.StartTime >= request.EndTime)
        {
            return BadRequest(new { error = "StartTime must be strictly earlier than EndTime." });
        }

        var groupExists = await _context.ActivityGroups.AnyAsync(ag => ag.Id == groupId, cancellationToken);
        if (!groupExists)
        {
            return NotFound(new { error = $"Activity group with ID '{groupId}' was not found." });
        }

        var existingMeetings = await _context.Meetings
            .Where(m => m.ActivityGroupId == groupId && m.DayOfWeek == request.DayOfWeek)
            .ToListAsync(cancellationToken);

        var newMeeting = new Meeting
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Room = request.Room?.Trim()
        };

        if (existingMeetings.Any(m => m.OverlapsWith(newMeeting)))
        {
            return BadRequest(new { error = "The new meeting overlaps with an existing meeting in the same activity group." });
        }

        await _context.Meetings.AddAsync(newMeeting, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new AdminMeetingDetailDto
        {
            Id = newMeeting.Id,
            DayOfWeek = newMeeting.DayOfWeek,
            StartTime = newMeeting.StartTime.ToString("HH:mm"),
            EndTime = newMeeting.EndTime.ToString("HH:mm"),
            Room = newMeeting.Room
        });
    }

    [HttpPut("meetings/{id:guid}")]
    public async Task<IActionResult> UpdateMeeting(
        Guid id,
        [FromBody] UpdateMeetingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        if (request.StartTime >= request.EndTime)
        {
            return BadRequest(new { error = "StartTime must be strictly earlier than EndTime." });
        }

        var meeting = await _context.Meetings.FindAsync([id], cancellationToken);
        if (meeting == null)
        {
            return NotFound(new { error = $"Meeting with ID '{id}' was not found." });
        }

        var existingMeetings = await _context.Meetings
            .Where(m => m.Id != id && m.ActivityGroupId == meeting.ActivityGroupId && m.DayOfWeek == request.DayOfWeek)
            .ToListAsync(cancellationToken);

        var hypothetical = new Meeting
        {
            Id = id,
            ActivityGroupId = meeting.ActivityGroupId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Room = request.Room?.Trim()
        };

        if (existingMeetings.Any(m => m.OverlapsWith(hypothetical)))
        {
            return BadRequest(new { error = "The updated meeting overlaps with an existing meeting in the same activity group." });
        }

        meeting.DayOfWeek = request.DayOfWeek;
        meeting.StartTime = request.StartTime;
        meeting.EndTime = request.EndTime;
        meeting.Room = request.Room?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new AdminMeetingDetailDto
        {
            Id = meeting.Id,
            DayOfWeek = meeting.DayOfWeek,
            StartTime = meeting.StartTime.ToString("HH:mm"),
            EndTime = meeting.EndTime.ToString("HH:mm"),
            Room = meeting.Room
        });
    }

    [HttpDelete("meetings/{id:guid}")]
    public async Task<IActionResult> DeleteMeeting(Guid id, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FindAsync([id], cancellationToken);
        if (meeting == null)
        {
            return NotFound(new { error = $"Meeting with ID '{id}' was not found." });
        }

        _context.Meetings.Remove(meeting);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
