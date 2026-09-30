namespace AegisSchedule.Api.DTOs;

public sealed class AcademicLevelDto
{
    public Guid Id { get; set; }
    public int LevelNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
