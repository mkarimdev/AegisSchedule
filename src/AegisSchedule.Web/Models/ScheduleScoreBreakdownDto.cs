namespace AegisSchedule.Web.Models;

public sealed class ScheduleScoreBreakdownDto
{
    public int TotalDays { get; set; }
    public double TotalGapHours { get; set; }
    public double TimeBlockAlignmentPercentage { get; set; }
    public double DaysScore { get; set; }
    public double GapsScore { get; set; }
    public double TimeBlockScore { get; set; }
    public double OverallScore { get; set; }
}
