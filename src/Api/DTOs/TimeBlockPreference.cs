using System.Text.Json.Serialization;

namespace Api.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeBlockPreference
{
    None = 0,
    Morning = 1,
    Afternoon = 2
}
