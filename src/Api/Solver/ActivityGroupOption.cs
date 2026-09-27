using Api.Domain;

namespace Api.Solver;

public sealed record ActivityGroupOption
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ActivityGroupId => Id;
    public Guid ActivityId { get; init; }
    public Guid CourseOfferingId { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public int AcademicLevelNumber { get; init; }
    public ActivityType ActivityType { get; init; } = ActivityType.Lecture;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<Meeting> Meetings { get; init; } = [];

    public ActivityGroupOption() { }

    public ActivityGroupOption(
        Guid id,
        string name,
        IReadOnlyList<Meeting> meetings,
        ActivityType activityType = ActivityType.Lecture,
        Guid courseOfferingId = default,
        string courseCode = "",
        int academicLevelNumber = 0,
        Guid activityId = default)
    {
        Id = id;
        Name = name;
        Meetings = meetings;
        ActivityType = activityType;
        CourseOfferingId = courseOfferingId;
        CourseCode = courseCode;
        AcademicLevelNumber = academicLevelNumber;
        ActivityId = activityId;
    }

    public static ActivityGroupOption FromDomain(
        ActivityGroup group,
        Guid courseOfferingId,
        ActivityType activityType,
        IReadOnlyList<Meeting> meetings,
        string courseCode = "",
        int academicLevelNumber = 0) =>
        new(
            id: group.Id,
            name: group.Name,
            meetings: meetings,
            activityType: activityType,
            courseOfferingId: courseOfferingId,
            courseCode: courseCode,
            academicLevelNumber: academicLevelNumber,
            activityId: group.ActivityId
        );
}
