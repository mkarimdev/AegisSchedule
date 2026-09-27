namespace Api.Solver;

public sealed record SchedulingResult
{
    public List<GeneratedSchedule> ValidSchedules { get; init; } = [];
    public bool IsSuccess { get; init; }
    public int TotalCombinationsEvaluated { get; init; }
    public string? ErrorMessage { get; init; }

    public static SchedulingResult Success(List<GeneratedSchedule> schedules, int combinationsEvaluated) =>
        new()
        {
            IsSuccess = true,
            ValidSchedules = schedules,
            TotalCombinationsEvaluated = combinationsEvaluated
        };

    public static SchedulingResult Failure(string errorMessage, int combinationsEvaluated = 0) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            TotalCombinationsEvaluated = combinationsEvaluated
        };
}
