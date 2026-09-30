namespace AegisSchedule.Web.Models;

public class AdminCourseDto
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public Guid AcademicLevelId { get; set; }
    public int AcademicLevelNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class CreateCourseRequest
{
    public Guid? UniversityId { get; set; }
    public Guid AcademicLevelId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UpdateCourseRequest
{
    public Guid AcademicLevelId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
