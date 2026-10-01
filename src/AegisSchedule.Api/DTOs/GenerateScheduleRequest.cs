using AegisSchedule.Api.Solver;

namespace AegisSchedule.Api.DTOs;

public sealed class GenerateScheduleRequest
{
    public Guid? StudentId { get; set; }
    public int AcademicLevelNumber { get; set; }
    public string? PrimaryLectureGroupName { get; set; }
    public string? PrimaryLabSectionName { get; set; }
    public List<Guid> SelectedCourseOfferingIds { get; set; } = [];
    public int MinimizeDaysWeight { get; set; } = 0;
    public int MinimizeGapsWeight { get; set; } = 0;
    public TimeBlockPreference PreferredTimeBlock { get; set; } = TimeBlockPreference.None;
    public int PreferredTimeBlockWeight { get; set; } = 0;
    public bool IsCombinationsCapExceeded { get; set; }
}
