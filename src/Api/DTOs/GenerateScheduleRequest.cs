namespace Api.DTOs;

public sealed class GenerateScheduleRequest
{
    public Guid? StudentId { get; set; }
    public int AcademicLevelNumber { get; set; }
    public string? PrimaryLectureGroupName { get; set; }
    public string? PrimaryLabSectionName { get; set; }
    public List<Guid> SelectedCourseOfferingIds { get; set; } = [];
}
