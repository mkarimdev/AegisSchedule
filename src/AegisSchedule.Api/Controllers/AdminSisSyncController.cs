using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Integrations;
using AegisSchedule.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace AegisSchedule.Api.Controllers;

[ApiController]
[ApiKeyAuth]
[Route("api/admin/sis")]
public class AdminSisSyncController : ControllerBase
{
    private readonly ISisImportService _sisImportService;

    public AdminSisSyncController(ISisImportService sisImportService)
    {
        _sisImportService = sisImportService;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SisBatchSyncResult>> SyncBatch(
        [FromBody] SisBatchSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        var result = await _sisImportService.SyncBatchAsync(request, cancellationToken);

        if (result.Success)
        {
            return Ok(result);
        }

        // Differentiate between 400 Bad Request (foreign reference not found) and 422 Unprocessable Entity (invariant/data validation violation)
        bool hasBadReference = result.Errors.Any(e =>
            e.Code is "UNIVERSITY_NOT_FOUND" or "NO_UNIVERSITY_CONFIGURED" or "ACADEMIC_LEVEL_NOT_FOUND");

        if (hasBadReference)
        {
            return BadRequest(result);
        }

        return UnprocessableEntity(result);
    }
}
