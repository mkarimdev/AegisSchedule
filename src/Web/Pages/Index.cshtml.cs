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
            _logger.LogWarning(ex, "Failed to load academic levels from API.");
            ErrorMessage = "Could not connect to the scheduling API. Please verify the API backend is running.";
        }
    }
}
