namespace AegisSchedule.Api.Solver;

public sealed record SchedulingResult
{
    public List<GeneratedSchedule> ValidSchedules { get; init; } = [];
    public bool IsSuccess { get; init; }
    public int TotalCombinationsEvaluated { get; init; }
    public bool IsCombinationsCapExceeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static SchedulingResult Success(List<GeneratedSchedule> schedules, int combinationsEvaluated, bool isCombinationsCapExceeded = false) =>
        new()
        {
            IsSuccess = true,
            ValidSchedules = schedules,
            TotalCombinationsEvaluated = combinationsEvaluated,
            IsCombinationsCapExceeded = isCombinationsCapExceeded
        };

    public static SchedulingResult Failure(string errorMessage, int combinationsEvaluated = 0, bool isCombinationsCapExceeded = false) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            TotalCombinationsEvaluated = combinationsEvaluated,
            IsCombinationsCapExceeded = isCombinationsCapExceeded
        };
}
