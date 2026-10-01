namespace AegisSchedule.Api.Solver;

public sealed class SolverOptions
{
    public const string SectionName = "Solver";

    /// <summary>
    /// Maximum number of candidate combinations evaluated before truncating search.
    /// Default: 50,000.
    /// </summary>
    public int MaxCombinations { get; set; } = 50_000;

    /// <summary>
    /// Maximum time budget in milliseconds before the solver terminates early.
    /// Default: 5,000 ms.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5_000;
}
