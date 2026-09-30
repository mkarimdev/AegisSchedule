using System.Text.Json.Serialization;

namespace AegisSchedule.Api.Solver;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeBlockPreference
{
    None = 0,
    Morning = 1,
    Afternoon = 2
}
