using AegisSchedule.Api.Domain;

namespace AegisSchedule.Api.Solver;

public static class ScheduleRankingEngine
{
    private static readonly TimeOnly MorningStart = new(8, 0);
    private static readonly TimeOnly MorningEnd = new(12, 30);
    private static readonly TimeOnly AfternoonStart = new(12, 30);
    private static readonly TimeOnly AfternoonEnd = new(17, 30);

    public static GeneratedSchedule ScoreSchedule(GeneratedSchedule schedule, SchedulePreferenceProfile preferences)
    {
        var meetings = schedule.AllMeetings;

        // 1. Days on campus calculation
        var distinctDays = meetings.Select(m => m.DayOfWeek).Distinct().ToList();
        int totalDays = distinctDays.Count == 0 ? 1 : distinctDays.Count;
        double daysScore = Math.Clamp(100.0 * (1.0 - (totalDays - 1.0) / 4.0), 0.0, 100.0);

        // 2. Idle gap calculation
        double totalGapMinutes = 0;
        foreach (var day in distinctDays)
        {
            var dayMeetings = meetings
                .Where(m => m.DayOfWeek == day)
                .OrderBy(m => m.StartTime)
                .ToList();

            for (int i = 0; i < dayMeetings.Count - 1; i++)
            {
                var current = dayMeetings[i];
                var next = dayMeetings[i + 1];

                if (next.StartTime > current.EndTime)
                {
                    totalGapMinutes += (next.StartTime - current.EndTime).TotalMinutes;
                }
            }
        }

        double totalGapHours = totalGapMinutes / 60.0;
        double gapsScore = Math.Clamp(100.0 - (totalGapHours * 15.0), 0.0, 100.0);

        // 3. Time block alignment calculation
        double alignmentPercentage = 100.0;
        double timeBlockScore = 100.0;

        if (preferences.PreferredTimeBlock != TimeBlockPreference.None && meetings.Count > 0)
        {
            var (windowStart, windowEnd) = preferences.PreferredTimeBlock == TimeBlockPreference.Morning
                ? (MorningStart, MorningEnd)
                : (AfternoonStart, AfternoonEnd);

            double totalMeetingMinutes = 0;
            double overlappingMinutes = 0;

            foreach (var meeting in meetings)
            {
                var duration = (meeting.EndTime - meeting.StartTime).TotalMinutes;
                totalMeetingMinutes += duration;

                var overlapStart = meeting.StartTime > windowStart ? meeting.StartTime : windowStart;
                var overlapEnd = meeting.EndTime < windowEnd ? meeting.EndTime : windowEnd;

                if (overlapEnd > overlapStart)
                {
                    overlappingMinutes += (overlapEnd - overlapStart).TotalMinutes;
                }
            }

            alignmentPercentage = totalMeetingMinutes > 0
                ? (overlappingMinutes / totalMeetingMinutes) * 100.0
                : 100.0;

            timeBlockScore = Math.Clamp(alignmentPercentage, 0.0, 100.0);
        }

        // 4. Weighted overall composite score
        int wDays = Math.Clamp(preferences.MinimizeDaysWeight, 0, 5);
        int wGaps = Math.Clamp(preferences.MinimizeGapsWeight, 0, 5);
        int wTime = 0;

        if (preferences.PreferredTimeBlock != TimeBlockPreference.None)
        {
            wTime = preferences.PreferredTimeBlockWeight > 0
                ? Math.Clamp(preferences.PreferredTimeBlockWeight, 0, 5)
                : 3;
        }

        int totalWeight = wDays + wGaps + wTime;
        double overallScore = totalWeight == 0
            ? 100.0
            : ((wDays * daysScore) + (wGaps * gapsScore) + (wTime * timeBlockScore)) / totalWeight;

        var breakdown = new ScheduleScoreBreakdown
        {
            TotalDays = totalDays,
            TotalGapHours = Math.Round(totalGapHours, 2),
            TimeBlockAlignmentPercentage = Math.Round(alignmentPercentage, 1),
            DaysScore = Math.Round(daysScore, 1),
            GapsScore = Math.Round(gapsScore, 1),
            TimeBlockScore = Math.Round(timeBlockScore, 1),
            OverallScore = Math.Round(overallScore, 1)
        };

        return schedule with
        {
            OverallScore = breakdown.OverallScore,
            ScoreBreakdown = breakdown
        };
    }

    public static List<GeneratedSchedule> RankAndSort(
        List<GeneratedSchedule> schedules,
        SchedulePreferenceProfile preferences)
    {
        if (schedules == null || schedules.Count == 0)
        {
            return [];
        }

        var scored = schedules
            .Select(s => ScoreSchedule(s, preferences))
            .OrderByDescending(s => s.OverallScore)
            .ThenBy(s => s.ScoreBreakdown.TotalDays)
            .ThenBy(s => s.ScoreBreakdown.TotalGapHours)
            .ThenBy(s => s.ScheduleId)
            .ToList();

        var ranked = new List<GeneratedSchedule>(scored.Count);
        for (int i = 0; i < scored.Count; i++)
        {
            ranked.Add(scored[i] with { Rank = i + 1 });
        }

        return ranked;
    }
}
