using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisSchedule.Api.Controllers;

[ApiController]
[Route("api/admin/courses")]
public class AdminCoursesController : ControllerBase
{
    private readonly UniSchedulingDbContext _context;

    public AdminCoursesController(UniSchedulingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminCourseDto>>> GetCourses(
        [FromQuery] int? academicLevel,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var query = from c in _context.Courses
                    join al in _context.AcademicLevels on c.AcademicLevelId equals al.Id
                    select new
                    {
                        Course = c,
                        LevelNumber = al.LevelNumber
                    };

        if (academicLevel.HasValue)
        {
            query = query.Where(x => x.LevelNumber == academicLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim().ToLower();
            query = query.Where(x => x.Course.Code.ToLower().Contains(trimmedSearch) ||
                                     x.Course.Name.ToLower().Contains(trimmedSearch));
        }

        var courses = await query
            .OrderBy(x => x.LevelNumber)
            .ThenBy(x => x.Course.Code)
            .Select(x => new AdminCourseDto
            {
                Id = x.Course.Id,
                UniversityId = x.Course.UniversityId,
                AcademicLevelId = x.Course.AcademicLevelId,
                AcademicLevelNumber = x.LevelNumber,
                Code = x.Course.Code,
                Name = x.Course.Name
            })
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminCourseDto>> GetCourseById(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await (from c in _context.Courses
                            where c.Id == id
                            join al in _context.AcademicLevels on c.AcademicLevelId equals al.Id
                            select new AdminCourseDto
                            {
                                Id = c.Id,
                                UniversityId = c.UniversityId,
                                AcademicLevelId = c.AcademicLevelId,
                                AcademicLevelNumber = al.LevelNumber,
                                Code = c.Code,
                                Name = c.Name
                            }).FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            return NotFound(new { error = $"Course with ID '{id}' was not found." });
        }

        return Ok(course);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCourseDto>> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { error = "Course code is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Course name is required." });
        }

        var level = await _context.AcademicLevels.FirstOrDefaultAsync(al => al.Id == request.AcademicLevelId, cancellationToken);
        if (level == null)
        {
            return BadRequest(new { error = $"AcademicLevel with ID '{request.AcademicLevelId}' does not exist." });
        }

        Guid universityId;
        if (request.UniversityId.HasValue && request.UniversityId.Value != Guid.Empty)
        {
            var uniExists = await _context.Universities.AnyAsync(u => u.Id == request.UniversityId.Value, cancellationToken);
            if (!uniExists)
            {
                return BadRequest(new { error = $"University with ID '{request.UniversityId.Value}' does not exist." });
            }
            universityId = request.UniversityId.Value;
        }
        else
        {
            var defaultUni = await _context.Universities.FirstOrDefaultAsync(cancellationToken);
            if (defaultUni == null)
            {
                return BadRequest(new { error = "No university found in the system to associate with this course." });
            }
            universityId = defaultUni.Id;
        }

        var codeNormalized = request.Code.Trim();
        var isDuplicate = await _context.Courses.AnyAsync(
            c => c.UniversityId == universityId && c.Code.ToLower() == codeNormalized.ToLower(),
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = $"Course code '{codeNormalized}' already exists." });
        }

        var course = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = universityId,
            AcademicLevelId = request.AcademicLevelId,
            Code = codeNormalized,
            Name = request.Name.Trim()
        };

        await _context.Courses.AddAsync(course, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new AdminCourseDto
        {
            Id = course.Id,
            UniversityId = course.UniversityId,
            AcademicLevelId = course.AcademicLevelId,
            AcademicLevelNumber = level.LevelNumber,
            Code = course.Code,
            Name = course.Name
        };

        return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCourseDto>> UpdateCourse(
        Guid id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        var course = await _context.Courses.FindAsync([id], cancellationToken);
        if (course == null)
        {
            return NotFound(new { error = $"Course with ID '{id}' was not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { error = "Course code is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Course name is required." });
        }

        var level = await _context.AcademicLevels.FirstOrDefaultAsync(al => al.Id == request.AcademicLevelId, cancellationToken);
        if (level == null)
        {
            return BadRequest(new { error = $"AcademicLevel with ID '{request.AcademicLevelId}' does not exist." });
        }

        var codeNormalized = request.Code.Trim();
        var isDuplicate = await _context.Courses.AnyAsync(
            c => c.Id != id && c.UniversityId == course.UniversityId && c.Code.ToLower() == codeNormalized.ToLower(),
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = $"Course code '{codeNormalized}' is already in use by another course." });
        }

        course.Code = codeNormalized;
        course.Name = request.Name.Trim();
        course.AcademicLevelId = request.AcademicLevelId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new AdminCourseDto
        {
            Id = course.Id,
            UniversityId = course.UniversityId,
            AcademicLevelId = course.AcademicLevelId,
            AcademicLevelNumber = level.LevelNumber,
            Code = course.Code,
            Name = course.Name
        };

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.FindAsync([id], cancellationToken);
        if (course == null)
        {
            return NotFound(new { error = $"Course with ID '{id}' was not found." });
        }

        var offerings = await _context.CourseOfferings
            .Where(co => co.CourseId == id)
            .ToListAsync(cancellationToken);

        if (offerings.Count > 0 && !force)
        {
            return BadRequest(new { error = "Cannot delete course because it has active course offerings. Use force=true to cascade delete." });
        }

        if (offerings.Count > 0 && force)
        {
            var offeringIds = offerings.Select(o => o.Id).ToList();
            var activities = await _context.Activities
                .Where(a => offeringIds.Contains(a.CourseOfferingId))
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
            _context.CourseOfferings.RemoveRange(offerings);
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
