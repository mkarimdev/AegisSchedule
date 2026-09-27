namespace Api.Domain;

public class CourseOffering
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid TermId { get; set; }
    public Guid UniversityId { get; set; }
}
