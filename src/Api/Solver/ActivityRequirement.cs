using Api.Domain;

namespace Api.Solver;

public sealed record ActivityRequirement
{
    public ActivityType Type { get; init; } = ActivityType.Lecture;
    public Guid? ActivityId { get; init; }

    public ActivityRequirement() { }

    public ActivityRequirement(ActivityType type, Guid? activityId = null)
    {
        Type = type;
        ActivityId = activityId;
    }

    public static implicit operator ActivityRequirement(ActivityType type) => new(type);
}
