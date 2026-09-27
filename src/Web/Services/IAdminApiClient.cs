using Web.Models;

namespace Web.Services;

public interface IAdminApiClient
{
    // Courses
    Task<List<AdminCourseDto>> GetCoursesAsync(int? academicLevel = null, string? search = null, CancellationToken cancellationToken = default);
    Task<AdminCourseDto?> GetCourseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminCourseDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default);
    Task<AdminCourseDto> UpdateCourseAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken = default);
    Task DeleteCourseAsync(Guid id, bool force = false, CancellationToken cancellationToken = default);

    // Offerings
    Task<List<CourseOfferingDto>> GetOfferingsAsync(Guid? termId = null, int? academicLevel = null, Guid? courseId = null, CancellationToken cancellationToken = default);
    Task<AdminOfferingDetailDto?> GetOfferingDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminOfferingDetailDto> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default);
    Task DeleteOfferingAsync(Guid id, CancellationToken cancellationToken = default);

    // Schedule Slots & Meetings
    Task<AdminActivityResponse> CreateActivityAsync(Guid offeringId, CreateActivityRequest request, CancellationToken cancellationToken = default);
    Task DeleteActivityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminActivityGroupResponse> CreateActivityGroupAsync(Guid activityId, CreateActivityGroupRequest request, CancellationToken cancellationToken = default);
    Task<AdminActivityGroupResponse> UpdateActivityGroupAsync(Guid id, UpdateActivityGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteActivityGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminMeetingDetailDto> CreateMeetingAsync(Guid groupId, CreateMeetingRequest request, CancellationToken cancellationToken = default);
    Task<AdminMeetingDetailDto> UpdateMeetingAsync(Guid id, UpdateMeetingRequest request, CancellationToken cancellationToken = default);
    Task DeleteMeetingAsync(Guid id, CancellationToken cancellationToken = default);

    // Terms
    Task<List<AdminTermDto>> GetTermsAsync(CancellationToken cancellationToken = default);
    Task<AdminTermDto> CreateTermAsync(CreateTermRequest request, CancellationToken cancellationToken = default);
    Task<AdminTermDto> SetCurrentTermAsync(Guid id, CancellationToken cancellationToken = default);
}
