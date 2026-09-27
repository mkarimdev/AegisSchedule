using Api.Domain;

namespace Api.Solver;

public class ScheduleSolver
{
    private sealed record TargetRequirement(
        SelectedCourseRequirement Course,
        ActivityType ActivityType,
        Guid? ActivityId,
        IReadOnlyList<ActivityGroupOption> Candidates
    );

    public SchedulingResult GenerateSchedules(SchedulingInputSnapshot snapshot)
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
        var currentSelected = new List<ActivityGroupOption>();
        var currentMeetings = new List<Meeting>();

        Backtrack(0, targets, currentSelected, currentMeetings, validSchedules, ref combinationsEvaluated);

        return SchedulingResult.Success(validSchedules, combinationsEvaluated);
    }

    public static SchedulingResult Solve(SchedulingInputSnapshot snapshot) =>
        new ScheduleSolver().GenerateSchedules(snapshot);

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

        // Rule (a): For same-level courses, automatically filter out ActivityGroup options that do not match AssignedPrimaryGroupId
        if (isSameLevel)
        {
            var primaryId = studentConstraint.AssignedPrimaryGroupId;
            var assignedIds = studentConstraint.AssignedGroupIds;

            bool hasPrimaryConstraint = (primaryId.HasValue && primaryId.Value != Guid.Empty) || assignedIds.Count > 0;

            if (hasPrimaryConstraint)
            {
                var filtered = candidates.Where(c =>
                    (primaryId.HasValue && (c.Id == primaryId.Value || c.ActivityGroupId == primaryId.Value)) ||
                    assignedIds.Contains(c.Id) ||
                    assignedIds.Contains(c.ActivityGroupId)
                ).ToList();

                if (filtered.Count > 0)
                {
                    candidates = filtered;
                }
                else
                {
                    // Check if the assigned group is present in another activity of this course
                    bool primaryBelongsToThisCourse = allOptions
                        .Where(o => MatchesCourse(o, course))
                        .Any(o => (primaryId.HasValue && (o.Id == primaryId.Value || o.ActivityGroupId == primaryId.Value)) ||
                                  assignedIds.Contains(o.Id) || assignedIds.Contains(o.ActivityGroupId));

                    if (!primaryBelongsToThisCourse)
                    {
                        // Assigned group was for this level/course but not found among options -> filter out
                        candidates = [];
                    }
                }
            }
        }
        // Rule (b): For cross-level courses, all candidates are preserved.

        return candidates;
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
        ref int combinationsEvaluated)
    {
        if (targetIndex == targets.Count)
        {
            combinationsEvaluated++;
            validSchedules.Add(new GeneratedSchedule(currentSelected.ToList()));
            return;
        }

        var target = targets[targetIndex];

        foreach (var candidate in target.Candidates)
        {
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

            Backtrack(targetIndex + 1, targets, currentSelected, currentMeetings, validSchedules, ref combinationsEvaluated);

            // Backtrack undo
            currentSelected.RemoveAt(currentSelected.Count - 1);
            int removeCount = candidate.Meetings.Count;
            currentMeetings.RemoveRange(currentMeetings.Count - removeCount, removeCount);
        }
    }
}
