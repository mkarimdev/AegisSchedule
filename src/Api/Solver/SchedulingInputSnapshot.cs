namespace Api.Solver;

public sealed record SchedulingInputSnapshot
{
    public StudentGroupConstraint StudentConstraint { get; init; } = new();
    public StudentGroupConstraint StudentGroupConstraint => StudentConstraint;

    public IReadOnlyList<SelectedCourseRequirement> SelectedCourses { get; init; } = [];
    public IReadOnlyList<SelectedCourseRequirement> CourseRequirements => SelectedCourses;

    public IReadOnlyList<ActivityGroupOption> AvailableOptions { get; init; } = [];
    public IReadOnlyList<ActivityGroupOption> AvailableGroups => AvailableOptions;

    public SchedulingInputSnapshot() { }

    public SchedulingInputSnapshot(
        StudentGroupConstraint studentConstraint,
        IReadOnlyList<SelectedCourseRequirement> selectedCourses,
        IReadOnlyList<ActivityGroupOption> availableOptions)
    {
        StudentConstraint = studentConstraint;
        SelectedCourses = selectedCourses;
        AvailableOptions = availableOptions;
    }
}
