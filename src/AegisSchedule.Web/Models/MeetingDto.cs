using System.Text.Json.Serialization;

namespace AegisSchedule.Web.Models;

public sealed class MeetingDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
}
