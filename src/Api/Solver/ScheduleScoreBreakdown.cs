namespace Api.Solver;

public sealed record ScheduleScoreBreakdown
{
    public int TotalDays { get; init; }
    public double TotalGapHours { get; init; }
    public double TimeBlockAlignmentPercentage { get; init; }
    public double DaysScore { get; init; }
    public double GapsScore { get; init; }
    public double TimeBlockScore { get; init; }
    public double OverallScore { get; init; }
}
