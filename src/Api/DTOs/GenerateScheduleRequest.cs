namespace Api.DTOs;

public sealed class GenerateScheduleRequest
{
    public Guid? StudentId { get; set; }
    public int AcademicLevelNumber { get; set; }
    public Guid? AssignedPrimaryGroupId { get; set; }
    public List<Guid> SelectedCourseOfferingIds { get; set; } = [];
}
