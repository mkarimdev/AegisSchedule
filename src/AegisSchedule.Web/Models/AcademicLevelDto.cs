namespace AegisSchedule.Web.Models;

public sealed class AcademicLevelDto
{
    public Guid Id { get; set; }
    public int LevelNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
