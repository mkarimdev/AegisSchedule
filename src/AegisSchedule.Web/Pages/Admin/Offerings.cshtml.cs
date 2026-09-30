using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AegisSchedule.Web.Models;
using AegisSchedule.Web.Services;

namespace AegisSchedule.Web.Pages.Admin;

public class OfferingsModel : PageModel
{
    private readonly IAdminApiClient _adminClient;
    private readonly ILogger<OfferingsModel> _logger;

    public List<AdminTermDto> Terms { get; set; } = [];
    public AdminTermDto? ActiveTerm => Terms.FirstOrDefault(t => t.IsCurrent);

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedTermId { get; set; }

    public List<CourseOfferingDto> Offerings { get; set; } = [];
    public List<AdminCourseDto> AvailableCourses { get; set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public OfferingsModel(IAdminApiClient adminClient, ILogger<OfferingsModel> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Terms = await _adminClient.GetTermsAsync();

            if (!SelectedTermId.HasValue || SelectedTermId.Value == Guid.Empty)
            {
                SelectedTermId = ActiveTerm?.Id ?? Terms.FirstOrDefault()?.Id;
            }

            if (SelectedTermId.HasValue && SelectedTermId.Value != Guid.Empty)
            {
                Offerings = await _adminClient.GetOfferingsAsync(termId: SelectedTermId.Value);

                var allCourses = await _adminClient.GetCoursesAsync();
                var offeredCourseIds = Offerings.Select(o => o.CourseId).ToHashSet();
                AvailableCourses = allCourses.Where(c => !offeredCourseIds.Contains(c.Id)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load offerings or terms.");
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateTermAsync(Semester semester, int year, bool isCurrent)
    {
        if (year <= 0)
        {
            ErrorMessage = "Valid year is required.";
            return RedirectToPage(new { SelectedTermId });
        }

        try
        {
            var created = await _adminClient.CreateTermAsync(new CreateTermRequest
            {
                Semester = semester,
                Year = year,
                IsCurrent = isCurrent
            });

            SuccessMessage = $"Term '{created.DisplayName}' created successfully.";
            return RedirectToPage(new { SelectedTermId = created.Id });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create term.");
            ErrorMessage = ex.Message;
            return RedirectToPage(new { SelectedTermId });
        }
    }

    public async Task<IActionResult> OnPostSetCurrentTermAsync(Guid termId)
    {
        if (termId == Guid.Empty)
        {
            ErrorMessage = "Invalid term ID.";
            return RedirectToPage(new { SelectedTermId });
        }

        try
        {
            var updated = await _adminClient.SetCurrentTermAsync(termId);
            SuccessMessage = $"Term '{updated.DisplayName}' is now the active registration term.";
            return RedirectToPage(new { SelectedTermId = termId });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set active term.");
            ErrorMessage = ex.Message;
            return RedirectToPage(new { SelectedTermId });
        }
    }

    public async Task<IActionResult> OnPostCreateOfferingAsync(Guid courseId)
    {
        if (!SelectedTermId.HasValue || SelectedTermId.Value == Guid.Empty || courseId == Guid.Empty)
        {
            ErrorMessage = "Course and Term are required to create an offering.";
            return RedirectToPage(new { SelectedTermId });
        }

        try
        {
            var created = await _adminClient.CreateOfferingAsync(new CreateOfferingRequest
            {
                CourseId = courseId,
                TermId = SelectedTermId.Value
            });

            SuccessMessage = $"Course '{created.CourseCode}' added to term offerings.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create course offering.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { SelectedTermId });
    }

    public async Task<IActionResult> OnPostDeleteOfferingAsync(Guid offeringId)
    {
        if (offeringId == Guid.Empty)
        {
            ErrorMessage = "Invalid offering ID.";
            return RedirectToPage(new { SelectedTermId });
        }

        try
        {
            await _adminClient.DeleteOfferingAsync(offeringId);
            SuccessMessage = "Course offering and all associated timetable slots were removed.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete course offering.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { SelectedTermId });
    }
}
