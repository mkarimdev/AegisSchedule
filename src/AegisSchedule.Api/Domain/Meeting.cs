namespace AegisSchedule.Api.Domain;

public class Meeting
{
    public Guid Id { get; set; }
    public Guid ActivityGroupId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }

    public bool OverlapsWith(Meeting? other)
    {
        if (other is null)
        {
            return false;
        }

        return DayOfWeek == other.DayOfWeek &&
               StartTime < other.EndTime &&
               other.StartTime < EndTime;
    }
}
