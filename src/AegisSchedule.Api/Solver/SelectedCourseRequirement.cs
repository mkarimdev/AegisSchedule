using AegisSchedule.Api.Domain;

namespace AegisSchedule.Api.Solver;

public sealed record SelectedCourseRequirement
{
    public Guid CourseOfferingId { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public int AcademicLevelNumber { get; init; }
    public IReadOnlyList<ActivityRequirement> ActivityRequirements { get; init; } = [];

    public SelectedCourseRequirement() { }

    public SelectedCourseRequirement(
        Guid courseOfferingId,
        string courseCode,
        IReadOnlyList<ActivityRequirement> activityRequirements,
        int academicLevelNumber = 0)
    {
        CourseOfferingId = courseOfferingId;
        CourseCode = courseCode;
        ActivityRequirements = activityRequirements;
        AcademicLevelNumber = academicLevelNumber;
    }
}
