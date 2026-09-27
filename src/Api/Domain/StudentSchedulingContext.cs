namespace Api.Domain;

public class StudentSchedulingContext
{
    public Guid StudentId { get; set; }
    public int AcademicLevelNumber { get; set; }
    public Guid PrimaryGroupId { get; set; }
    public List<Guid> SelectedCourseOfferingIds { get; set; } = [];
    public Dictionary<Guid, Guid> CrossLevelGroupSelections { get; set; } = [];
}
