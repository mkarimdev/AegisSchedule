namespace AegisSchedule.Api.Domain;

public class Course
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public Guid AcademicLevelId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
