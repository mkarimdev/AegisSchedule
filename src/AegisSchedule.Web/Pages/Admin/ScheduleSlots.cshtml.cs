using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AegisSchedule.Web.Models;
using AegisSchedule.Web.Services;

namespace AegisSchedule.Web.Pages.Admin;

public class ScheduleSlotsModel : PageModel
{
    private readonly IAdminApiClient _adminClient;
    private readonly ILogger<ScheduleSlotsModel> _logger;

    public List<CourseOfferingDto> AllOfferings { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? OfferingId { get; set; }

    public AdminOfferingDetailDto? OfferingDetail { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public ScheduleSlotsModel(IAdminApiClient adminClient, ILogger<ScheduleSlotsModel> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            AllOfferings = await _adminClient.GetOfferingsAsync();

            if (!OfferingId.HasValue || OfferingId.Value == Guid.Empty)
            {
                OfferingId = AllOfferings.FirstOrDefault()?.Id;
            }

            if (OfferingId.HasValue && OfferingId.Value != Guid.Empty)
            {
                OfferingDetail = await _adminClient.GetOfferingDetailAsync(OfferingId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schedule slot details.");
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateActivityAsync(Guid offeringId, ActivityType type)
    {
        try
        {
            await _adminClient.CreateActivityAsync(offeringId, new CreateActivityRequest { Type = type });
            SuccessMessage = $"Added activity category '{type}'.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create activity.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { OfferingId = offeringId });
    }

    public async Task<IActionResult> OnPostDeleteActivityAsync(Guid offeringId, Guid activityId)
    {
        try
        {
            await _adminClient.DeleteActivityAsync(activityId);
            SuccessMessage = "Activity category and its sections removed.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete activity.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { OfferingId = offeringId });
    }

    public async Task<IActionResult> OnPostCreateGroupAsync(Guid offeringId, Guid activityId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Section group name is required.";
            return RedirectToPage(new { OfferingId = offeringId });
        }

        try
        {
            await _adminClient.CreateActivityGroupAsync(activityId, new CreateActivityGroupRequest { Name = name.Trim() });
            SuccessMessage = $"Section group '{name.Trim()}' added.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create section group.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { OfferingId = offeringId });
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid offeringId, Guid groupId)
    {
        try
        {
            await _adminClient.DeleteActivityGroupAsync(groupId);
            SuccessMessage = "Section group and its meetings removed.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete group.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { OfferingId = offeringId });
    }

    public async Task<IActionResult> OnPostCreateMeetingAsync(Guid offeringId, Guid groupId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, string? room)
    {
        if (startTime >= endTime)
        {
            ErrorMessage = "Start time must be strictly earlier than end time.";
            return RedirectToPage(new { OfferingId = offeringId });
        }

        try
        {
            await _adminClient.CreateMeetingAsync(groupId, new CreateMeetingRequest
            {
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                Room = room?.Trim()
            });

            SuccessMessage = $"Meeting slot added on {dayOfWeek} ({startTime:HH:mm} - {endTime:HH:mm}).";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create meeting slot.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { OfferingId = offeringId });
    }

    public async Task<IActionResult> OnPostDeleteMeetingAsync(Guid offeringId, Guid meetingId)
    {
        try
        {
            await _adminClient.DeleteMeetingAsync(meetingId);
            SuccessMessage = "Meeting slot deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete meeting.");
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { OfferingId = offeringId });
    }
}
