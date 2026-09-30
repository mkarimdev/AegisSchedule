using Api.Domain;

namespace Api.Solver;

public sealed record GeneratedSchedule
{
    public Guid ScheduleId { get; init; } = Guid.NewGuid();
    public int Rank { get; init; } = 1;
    public double OverallScore { get; init; } = 100.0;
    public ScheduleScoreBreakdown ScoreBreakdown { get; init; } = new();
    public IReadOnlyList<ActivityGroupOption> SelectedGroups { get; init; } = [];
    public IReadOnlyList<ActivityGroupOption> ActivityGroups => SelectedGroups;
    public IReadOnlyList<Meeting> AllMeetings => SelectedGroups.SelectMany(g => g.Meetings).ToList();

    public GeneratedSchedule() { }

    public GeneratedSchedule(IReadOnlyList<ActivityGroupOption> selectedGroups)
    {
        SelectedGroups = selectedGroups;
    }
}
