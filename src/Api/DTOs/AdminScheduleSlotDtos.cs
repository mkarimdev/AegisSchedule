using System.Text.Json.Serialization;
using Api.Domain;

namespace Api.DTOs;

public class CreateActivityRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
}

public class UpdateMeetingRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
