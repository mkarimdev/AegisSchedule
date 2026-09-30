using Api.DTOs;

namespace Api.Solver;

public sealed record SchedulePreferenceProfile(
    int MinimizeDaysWeight = 0,
    int MinimizeGapsWeight = 0,
    TimeBlockPreference PreferredTimeBlock = TimeBlockPreference.None,
    int PreferredTimeBlockWeight = 0
);
