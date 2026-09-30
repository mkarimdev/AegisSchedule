using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AegisSchedule.Web.Models;
using AegisSchedule.Web.Services;

namespace AegisSchedule.Web.Pages.Admin;

public class CoursesModel : PageModel
{
    private readonly IAdminApiClient _adminClient;
    private readonly ISchedulingApiClient _schedulingClient;
    private readonly ILogger<CoursesModel> _logger;

    public List<AdminCourseDto> Courses { get; set; } = [];
    public List<AcademicLevelDto> AcademicLevels { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? AcademicLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public CoursesModel(IAdminApiClient adminClient, ISchedulingApiClient schedulingClient, ILogger<CoursesModel> logger)
    {
        _adminClient = adminClient;
        _schedulingClient = schedulingClient;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            AcademicLevels = await _schedulingClient.GetLevelsAsync();
            Courses = await _adminClient.GetCoursesAsync(AcademicLevel, Search);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load courses or academic levels.");
            ErrorMessage = $"Failed to load catalog data: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(string code, string name, Guid academicLevelId)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || academicLevelId == Guid.Empty)
        {
            ErrorMessage = "Course Code, Course Name, and Academic Level are required.";
            return RedirectToPage(new { AcademicLevel, Search });
        }

        try
        {
            await _adminClient.CreateCourseAsync(new CreateCourseRequest
            {
                Code = code.Trim(),
                Name = name.Trim(),
                AcademicLevelId = academicLevelId
            });

            SuccessMessage = $"Course '{code.Trim().ToUpper()}' created successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create course.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { AcademicLevel, Search });
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, string code, string name, Guid academicLevelId)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || academicLevelId == Guid.Empty)
        {
            ErrorMessage = "Invalid update payload.";
            return RedirectToPage(new { AcademicLevel, Search });
        }

        try
        {
            await _adminClient.UpdateCourseAsync(id, new UpdateCourseRequest
            {
                Code = code.Trim(),
                Name = name.Trim(),
                AcademicLevelId = academicLevelId
            });

            SuccessMessage = $"Course '{code.Trim().ToUpper()}' updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update course {CourseId}.", id);
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { AcademicLevel, Search });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, bool force = false)
    {
        if (id == Guid.Empty)
        {
            ErrorMessage = "Invalid course ID.";
            return RedirectToPage(new { AcademicLevel, Search });
        }

        try
        {
            await _adminClient.DeleteCourseAsync(id, force);
            SuccessMessage = "Course deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete course {CourseId}.", id);
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { AcademicLevel, Search });
    }
}
