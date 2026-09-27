using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web.Models;
using Web.Services;

namespace Web.Pages;

public class IndexModel : PageModel
{
    private readonly ISchedulingApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    public List<AcademicLevelDto> AcademicLevels { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public IndexModel(ISchedulingApiClient apiClient, ILogger<IndexModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            AcademicLevels = await _apiClient.GetLevelsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load academic levels on initial page load.");
            ErrorMessage = "Could not connect to the scheduling API backend. Please ensure the API is running.";
        }
    }

    public async Task<IActionResult> OnGetLevelsAsync()
    {
        try
        {
            var levels = await _apiClient.GetLevelsAsync();
            return new JsonResult(levels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching academic levels.");
            return StatusCode(500, new { error = "Failed to fetch academic levels from the API." });
        }
    }

    public async Task<IActionResult> OnGetOfferingsAsync(int? levelNumber, Guid? termId)
    {
        try
        {
            var offerings = await _apiClient.GetOfferingsAsync(levelNumber, termId);
            return new JsonResult(offerings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching offerings for level {LevelNumber}", levelNumber);
            return StatusCode(500, new { error = "Failed to fetch offerings from the scheduling API." });
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync([FromBody] GenerateScheduleRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request payload cannot be empty." });
        }

        try
        {
            var result = await _apiClient.GenerateSchedulesAsync(request);
            return new JsonResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating schedules.");
            return StatusCode(500, new { error = "Failed to compute schedules from the API." });
        }
    }
}
