using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisSchedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly UniSchedulingDbContext _context;

    public CatalogController(UniSchedulingDbContext context)
    {
        _context = context;
    }

    [HttpGet("levels")]
    public async Task<ActionResult<List<AcademicLevelDto>>> GetLevels(CancellationToken cancellationToken = default)
    {
        var levels = await _context.AcademicLevels
            .OrderBy(al => al.LevelNumber)
            .Select(al => new AcademicLevelDto
            {
                Id = al.Id,
                LevelNumber = al.LevelNumber,
                DisplayName = $"Level {al.LevelNumber}"
            })
            .ToListAsync(cancellationToken);

        return Ok(levels);
    }

    [HttpGet("offerings")]
    public async Task<ActionResult<List<CourseOfferingDto>>> GetOfferings(
        [FromQuery] int? academicLevel,
        [FromQuery] Guid? termId,
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

        if (academicLevel.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.AcademicLevelNumber == academicLevel.Value);
        }

        if (termId.HasValue && termId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.TermId == termId.Value);
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
}
