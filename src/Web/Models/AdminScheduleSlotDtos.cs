namespace Web.Models;

public enum ActivityType
{
    Lecture = 0,
    Lab = 1,
    Tutorial = 2
}

public class CreateActivityRequest
{
    public ActivityType Type { get; set; }
}

public class CreateActivityGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateActivityGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateMeetingRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
}

public class UpdateMeetingRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
}

public class AdminActivityResponse
{
    public Guid Id { get; set; }
    public Guid CourseOfferingId { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class AdminActivityGroupResponse
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public string Name { get; set; } = string.Empty;
}
