namespace AegisSchedule.Api.Solver;

public sealed record StudentGroupConstraint
{
    public int AcademicLevelNumber { get; init; }
    public string? PrimaryLectureGroupName { get; init; }
    public string? PrimaryLabSectionName { get; init; }

    public StudentGroupConstraint() { }

    public StudentGroupConstraint(
        int academicLevelNumber,
        string? primaryLectureGroupName = null,
        string? primaryLabSectionName = null)
    {
        AcademicLevelNumber = academicLevelNumber;
        PrimaryLectureGroupName = string.IsNullOrWhiteSpace(primaryLectureGroupName) ? null : primaryLectureGroupName.Trim();
        PrimaryLabSectionName = string.IsNullOrWhiteSpace(primaryLabSectionName) ? null : primaryLabSectionName.Trim();
    }
}
