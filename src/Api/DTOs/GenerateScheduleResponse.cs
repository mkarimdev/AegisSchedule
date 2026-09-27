namespace Api.DTOs;

public sealed class GenerateScheduleResponse
{
    public bool IsSuccess { get; set; }
    public int TotalCombinationsEvaluated { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ScheduleDto> Schedules { get; set; } = [];
}
