using System.Diagnostics;
using AegisSchedule.Api.Domain;
using Microsoft.Extensions.Options;

namespace AegisSchedule.Api.Solver;

public class ScheduleSolver
{
    private readonly SolverOptions _options;

    public ScheduleSolver(IOptions<SolverOptions>? options = null)
    {
        _options = options?.Value ?? new SolverOptions();
    }

    public ScheduleSolver(SolverOptions options)
    {
        _options = options ?? new SolverOptions();
    }

    private sealed record TargetRequirement(
        SelectedCourseRequirement Course,
        ActivityType ActivityType,
        Guid? ActivityId,
        IReadOnlyList<ActivityGroupOption> Candidates
    );

    public SchedulingResult GenerateSchedules(SchedulingInputSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot == null)
        {
            return SchedulingResult.Failure("Snapshot cannot be null.");
        }

        if (snapshot.SelectedCourses.Count == 0)
        {
            return SchedulingResult.Success([], 0);
        }

        var targets = new List<TargetRequirement>();

        foreach (var course in snapshot.SelectedCourses)
        {
            int courseLevel = course.AcademicLevelNumber;

            // If explicit activity requirements are provided
            if (course.ActivityRequirements.Count > 0)
            {
                foreach (var actReq in course.ActivityRequirements)
                {
                    var candidates = FindCandidates(
                        snapshot.AvailableOptions,
                        course,
                        actReq.Type,
                        actReq.ActivityId,
                        courseLevel,
                        snapshot.StudentConstraint
                    );

                    targets.Add(new TargetRequirement(course, actReq.Type, actReq.ActivityId, candidates));
                }
            }
            else
            {
                // Infer distinct activities from AvailableOptions for this course
                var courseOptions = snapshot.AvailableOptions
                    .Where(o => MatchesCourse(o, course))
                    .ToList();

                var grouped = courseOptions
                    .GroupBy(o => (ActivityId: o.ActivityId != Guid.Empty ? o.ActivityId : Guid.Empty, o.ActivityType))
                    .OrderBy(g => g.Key.ActivityType);

                foreach (var group in grouped)
                {
                    var actType = group.Key.ActivityType;
                    var actId = group.Key.ActivityId != Guid.Empty ? group.Key.ActivityId : (Guid?)null;

                    var candidates = FindCandidates(
                        snapshot.AvailableOptions,
                        course,
                        actType,
                        actId,
                        courseLevel,
                        snapshot.StudentConstraint
                    );

                    targets.Add(new TargetRequirement(course, actType, actId, candidates));
                }
            }
        }

        // If any requirement has zero candidates, no complete valid schedule can be formed
        if (targets.Any(t => t.Candidates.Count == 0))
        {
            return SchedulingResult.Success([], 0);
        }

        var validSchedules = new List<GeneratedSchedule>();
        int combinationsEvaluated = 0;
        bool capExceeded = false;
        var currentSelected = new List<ActivityGroupOption>();
        var currentMeetings = new List<Meeting>();
        var stopwatch = Stopwatch.StartNew();

        Backtrack(
            0,
            targets,
            currentSelected,
            currentMeetings,
            validSchedules,
            ref combinationsEvaluated,
            ref capExceeded,
            stopwatch,
            _options,
            cancellationToken);

        var rankedSchedules = ScheduleRankingEngine.RankAndSort(validSchedules, snapshot.Preferences);

        return SchedulingResult.Success(rankedSchedules, combinationsEvaluated, capExceeded);
    }

    public static SchedulingResult Solve(SchedulingInputSnapshot snapshot, CancellationToken cancellationToken = default) =>
        new ScheduleSolver().GenerateSchedules(snapshot, cancellationToken);

    private static List<ActivityGroupOption> FindCandidates(
        IReadOnlyList<ActivityGroupOption> allOptions,
        SelectedCourseRequirement course,
        ActivityType actType,
        Guid? activityId,
        int courseLevel,
        StudentGroupConstraint studentConstraint)
    {
        var candidates = allOptions
            .Where(o => MatchesCourse(o, course))
            .Where(o => o.ActivityType == actType)
            .Where(o => !activityId.HasValue || activityId.Value == Guid.Empty || o.ActivityId == Guid.Empty || o.ActivityId == activityId.Value)
            .OrderBy(o => o.Name)
            .ThenBy(o => o.Id)
            .ToList();

        // Resolve effective level
        int effectiveLevel = courseLevel;
        if (effectiveLevel == 0)
        {
            var firstLevel = candidates.FirstOrDefault(c => c.AcademicLevelNumber != 0)?.AcademicLevelNumber;
            if (firstLevel.HasValue)
            {
                effectiveLevel = firstLevel.Value;
            }
        }

        bool isSameLevel = effectiveLevel != 0 && effectiveLevel == studentConstraint.AcademicLevelNumber;

        // Rule (a): For same-level courses, apply dual cohort locks (PrimaryLectureGroupName and PrimaryLabSectionName)
        if (isSameLevel)
        {
            if (actType == ActivityType.Lecture && !string.IsNullOrWhiteSpace(studentConstraint.PrimaryLectureGroupName))
            {
                var targetLecture = studentConstraint.PrimaryLectureGroupName;
                var matched = candidates.Where(c => MatchesGroupName(c.Name, targetLecture)).ToList();

                // If matching lecture groups exist for this course offering, lock to them.
                // Fallback: If course offering does not define that group (e.g. single general lecture), keep candidates.
                if (matched.Count > 0)
                {
                    candidates = matched;
                }
            }
            else if (actType == ActivityType.Lab && !string.IsNullOrWhiteSpace(studentConstraint.PrimaryLabSectionName))
            {
                var targetLab = studentConstraint.PrimaryLabSectionName;
                var matched = candidates.Where(c => MatchesGroupName(c.Name, targetLab)).ToList();

                if (matched.Count > 0)
                {
                    candidates = matched;
                }
            }
        }
        // Rule (b): For cross-level courses, all candidates are preserved without cohort filtering.

        return candidates;
    }

    private static bool MatchesGroupName(string candidateName, string targetName)
    {
        if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(targetName))
        {
            return false;
        }

        var cand = candidateName.Trim();
        var target = targetName.Trim();

        if (string.Equals(cand, target, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        static string Normalize(string s) =>
            s.Replace("Lecture", "", StringComparison.OrdinalIgnoreCase)
             .Replace("Lab", "", StringComparison.OrdinalIgnoreCase)
             .Trim();

        var normCand = Normalize(cand);
        var normTarget = Normalize(target);

        if (!string.IsNullOrEmpty(normCand) && !string.IsNullOrEmpty(normTarget) &&
            string.Equals(normCand, normTarget, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (cand.EndsWith($"-{target}", StringComparison.OrdinalIgnoreCase) ||
            cand.EndsWith($" {target}", StringComparison.OrdinalIgnoreCase) ||
            cand.EndsWith($"_{target}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool MatchesCourse(ActivityGroupOption option, SelectedCourseRequirement course)
    {
        if (course.CourseOfferingId != Guid.Empty && option.CourseOfferingId != Guid.Empty)
        {
            return course.CourseOfferingId == option.CourseOfferingId;
        }

        if (!string.IsNullOrEmpty(course.CourseCode) && !string.IsNullOrEmpty(option.CourseCode))
        {
            return string.Equals(course.CourseCode, option.CourseCode, StringComparison.OrdinalIgnoreCase);
        }

        if (course.CourseOfferingId != Guid.Empty && option.CourseOfferingId == Guid.Empty && !string.IsNullOrEmpty(option.CourseCode))
        {
            return string.Equals(course.CourseCode, option.CourseCode, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static void Backtrack(
        int targetIndex,
        List<TargetRequirement> targets,
        List<ActivityGroupOption> currentSelected,
        List<Meeting> currentMeetings,
        List<GeneratedSchedule> validSchedules,
        ref int combinationsEvaluated,
        ref bool capExceeded,
        Stopwatch stopwatch,
        SolverOptions options,
        CancellationToken cancellationToken)
    {
        if (capExceeded || cancellationToken.IsCancellationRequested)
        {
            capExceeded = true;
            return;
        }

        if (combinationsEvaluated >= options.MaxCombinations || stopwatch.ElapsedMilliseconds >= options.TimeoutMilliseconds)
        {
            capExceeded = true;
            return;
        }

        if (targetIndex == targets.Count)
        {
            combinationsEvaluated++;
            validSchedules.Add(new GeneratedSchedule(currentSelected.ToList()));
            return;
        }

        var target = targets[targetIndex];

        foreach (var candidate in target.Candidates)
        {
            if (capExceeded || cancellationToken.IsCancellationRequested)
            {
                capExceeded = true;
                return;
            }

            if (combinationsEvaluated >= options.MaxCombinations || stopwatch.ElapsedMilliseconds >= options.TimeoutMilliseconds)
            {
                capExceeded = true;
                return;
            }

            bool hasConflict = false;

            // Internal group meeting overlap check
            for (int i = 0; i < candidate.Meetings.Count; i++)
            {
                for (int j = i + 1; j < candidate.Meetings.Count; j++)
                {
                    if (candidate.Meetings[i].OverlapsWith(candidate.Meetings[j]))
                    {
                        hasConflict = true;
                        break;
                    }
                }
                if (hasConflict) break;
            }

            // Conflict check with already selected meetings
            if (!hasConflict)
            {
                foreach (var candMeeting in candidate.Meetings)
                {
                    foreach (var existingMeeting in currentMeetings)
                    {
                        if (candMeeting.OverlapsWith(existingMeeting))
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                    if (hasConflict) break;
                }
            }

            if (hasConflict)
            {
                combinationsEvaluated++;
                continue;
            }

            // Valid candidate branch
            currentSelected.Add(candidate);
            currentMeetings.AddRange(candidate.Meetings);

            Backtrack(
                targetIndex + 1,
                targets,
                currentSelected,
                currentMeetings,
                validSchedules,
                ref combinationsEvaluated,
                ref capExceeded,
                stopwatch,
                options,
                cancellationToken);

            // Backtrack undo
            currentSelected.RemoveAt(currentSelected.Count - 1);
            int removeCount = candidate.Meetings.Count;
            currentMeetings.RemoveRange(currentMeetings.Count - removeCount, removeCount);
        }
    }
}
