namespace Api.Solver;

public sealed record StudentGroupConstraint
{
    public int AcademicLevelNumber { get; init; }
    public Guid? AssignedPrimaryGroupId { get; init; }
    public IReadOnlyList<Guid> AssignedGroupIds { get; init; } = [];

    public StudentGroupConstraint() { }

    public StudentGroupConstraint(int academicLevelNumber, Guid? assignedPrimaryGroupId = null)
    {
        AcademicLevelNumber = academicLevelNumber;
        AssignedPrimaryGroupId = assignedPrimaryGroupId;
        if (assignedPrimaryGroupId.HasValue && assignedPrimaryGroupId.Value != Guid.Empty)
        {
            AssignedGroupIds = [assignedPrimaryGroupId.Value];
        }
    }

    public StudentGroupConstraint(int academicLevelNumber, IEnumerable<Guid> assignedGroupIds)
    {
        AcademicLevelNumber = academicLevelNumber;
        AssignedGroupIds = assignedGroupIds.ToList();
        AssignedPrimaryGroupId = AssignedGroupIds.FirstOrDefault();
    }
}
