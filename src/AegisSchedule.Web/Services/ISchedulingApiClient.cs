using AegisSchedule.Web.Models;

namespace AegisSchedule.Web.Services;

public interface ISchedulingApiClient
{
    Task<List<AcademicLevelDto>> GetLevelsAsync(CancellationToken cancellationToken = default);
    Task<List<CourseOfferingDto>> GetOfferingsAsync(int? academicLevel = null, Guid? termId = null, CancellationToken cancellationToken = default);
    Task<GenerateScheduleResponse> GenerateSchedulesAsync(GenerateScheduleRequest request, CancellationToken cancellationToken = default);
}
