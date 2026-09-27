namespace Web.Models;

public sealed class CourseOfferingDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public Guid AcademicLevelId { get; set; }
    public int AcademicLevelNumber { get; set; }
    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public List<string> ActivityTypes { get; set; } = [];
    public List<CatalogActivityGroupDto> Groups { get; set; } = [];
}
