namespace AegisSchedule.Api.Solver;

public sealed record SchedulingInputSnapshot
{
    public StudentGroupConstraint StudentConstraint { get; init; } = new();
    public StudentGroupConstraint StudentGroupConstraint => StudentConstraint;

    public SchedulePreferenceProfile Preferences { get; init; } = new();

    public IReadOnlyList<SelectedCourseRequirement> SelectedCourses { get; init; } = [];
    public IReadOnlyList<SelectedCourseRequirement> CourseRequirements => SelectedCourses;

    public IReadOnlyList<ActivityGroupOption> AvailableOptions { get; init; } = [];
    public IReadOnlyList<ActivityGroupOption> AvailableGroups => AvailableOptions;

    public SchedulingInputSnapshot() { }

    public SchedulingInputSnapshot(
        StudentGroupConstraint studentConstraint,
        IReadOnlyList<SelectedCourseRequirement> selectedCourses,
        IReadOnlyList<ActivityGroupOption> availableOptions,
        SchedulePreferenceProfile? preferences = null)
    {
        StudentConstraint = studentConstraint;
        SelectedCourses = selectedCourses;
        AvailableOptions = availableOptions;
        Preferences = preferences ?? new();
    }
}
