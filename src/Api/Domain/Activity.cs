namespace Api.Domain;

public class Activity
{
    public Guid Id { get; set; }
    public Guid CourseOfferingId { get; set; }
    public ActivityType Type { get; set; }
}
