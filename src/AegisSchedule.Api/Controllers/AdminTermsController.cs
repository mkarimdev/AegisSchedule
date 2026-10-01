using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using AegisSchedule.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisSchedule.Api.Controllers;

[ApiController]
[ApiKeyAuth]
[Route("api/admin/terms")]
public class AdminTermsController : ControllerBase
{
    private readonly UniSchedulingDbContext _context;

    public AdminTermsController(UniSchedulingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminTermDto>>> GetTerms(CancellationToken cancellationToken = default)
    {
        var terms = await _context.Terms
            .OrderByDescending(t => t.Year)
            .ThenBy(t => t.Semester)
            .Select(t => new AdminTermDto
            {
                Id = t.Id,
                Semester = t.Semester,
                Year = t.Year,
                IsCurrent = t.IsCurrent
            })
            .ToListAsync(cancellationToken);

        return Ok(terms);
    }

    [HttpPost]
    public async Task<ActionResult<AdminTermDto>> CreateTerm(
        [FromBody] CreateTermRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        if (request.Year <= 0)
        {
            return BadRequest(new { error = "Year must be a positive integer." });
        }

        var isDuplicate = await _context.Terms.AnyAsync(
            t => t.Semester == request.Semester && t.Year == request.Year,
            cancellationToken);

        if (isDuplicate)
        {
            return BadRequest(new { error = $"Term {request.Semester} {request.Year} already exists." });
        }

        if (request.IsCurrent)
        {
            var existingCurrentTerms = await _context.Terms
                .Where(t => t.IsCurrent)
                .ToListAsync(cancellationToken);

            foreach (var term in existingCurrentTerms)
            {
                term.IsCurrent = false;
            }
        }

        var newTerm = new Term
        {
            Id = Guid.NewGuid(),
            Semester = request.Semester,
            Year = request.Year,
            IsCurrent = request.IsCurrent
        };

        await _context.Terms.AddAsync(newTerm, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new AdminTermDto
        {
            Id = newTerm.Id,
            Semester = newTerm.Semester,
            Year = newTerm.Year,
            IsCurrent = newTerm.IsCurrent
        };

        return StatusCode(201, dto);
    }

    [HttpPut("{id:guid}/set-current")]
    public async Task<ActionResult<AdminTermDto>> SetCurrentTerm(Guid id, CancellationToken cancellationToken = default)
    {
        var targetTerm = await _context.Terms.FindAsync([id], cancellationToken);
        if (targetTerm == null)
        {
            return NotFound(new { error = $"Term with ID '{id}' was not found." });
        }

        var allTerms = await _context.Terms.ToListAsync(cancellationToken);
        foreach (var term in allTerms)
        {
            term.IsCurrent = (term.Id == id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new AdminTermDto
        {
            Id = targetTerm.Id,
            Semester = targetTerm.Semester,
            Year = targetTerm.Year,
            IsCurrent = targetTerm.IsCurrent
        };

        return Ok(dto);
    }
}
