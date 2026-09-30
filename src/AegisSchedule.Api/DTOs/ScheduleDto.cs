namespace AegisSchedule.Api.DTOs;

public sealed class ScheduleDto
{
    public Guid ScheduleId { get; set; }
    public int Rank { get; set; } = 1;
    public double OverallScore { get; set; } = 100.0;
    public ScheduleScoreBreakdownDto ScoreBreakdown { get; set; } = new();
    public List<SelectedGroupDto> SelectedGroups { get; set; } = [];
}
