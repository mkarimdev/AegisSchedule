namespace AegisSchedule.Api.Domain;

public class ActivityGroup
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public string Name { get; set; } = string.Empty;
}
