using AegisSchedule.Api.Domain;
using AegisSchedule.Api.Solver;
using Xunit;

namespace AegisSchedule.Tests;

public class SolverSafetyTests
{
    [Fact]
    public void GenerateSchedules_WhenMaxCombinationsExceeded_TruncatesSearchAndSetsCapFlag()
    {
        // Arrange
        // Create 3 courses with 3 non-conflicting sections each = 3 * 3 * 3 = 27 combinations
        var course1OfferingId = Guid.NewGuid();
        var course2OfferingId = Guid.NewGuid();
        var course3OfferingId = Guid.NewGuid();

        var course1 = new SelectedCourseRequirement(
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        var course2 = new SelectedCourseRequirement(
            courseOfferingId: course2OfferingId,
            courseCode: "MATH101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        var course3 = new SelectedCourseRequirement(
            courseOfferingId: course3OfferingId,
            courseCode: "PHYS101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        var availableOptions = new List<ActivityGroupOption>();

        // Course 1 sections: Sunday 8-10, 10-12, 12-14
        for (int i = 0; i < 3; i++)
        {
            availableOptions.Add(new ActivityGroupOption(
                id: Guid.NewGuid(),
                name: $"CS101-G{i + 1}",
                meetings:
                [
                    new Meeting
                    {
                        DayOfWeek = DayOfWeek.Sunday,
                        StartTime = new TimeOnly(8 + i * 2, 0),
                        EndTime = new TimeOnly(10 + i * 2, 0)
                    }
                ],
                activityType: ActivityType.Lecture,
                courseOfferingId: course1OfferingId,
                courseCode: "CS101",
                academicLevelNumber: 1
            ));
        }

        // Course 2 sections: Monday 8-10, 10-12, 12-14
        for (int i = 0; i < 3; i++)
        {
            availableOptions.Add(new ActivityGroupOption(
                id: Guid.NewGuid(),
                name: $"MATH101-G{i + 1}",
                meetings:
                [
                    new Meeting
                    {
                        DayOfWeek = DayOfWeek.Monday,
                        StartTime = new TimeOnly(8 + i * 2, 0),
                        EndTime = new TimeOnly(10 + i * 2, 0)
                    }
                ],
                activityType: ActivityType.Lecture,
                courseOfferingId: course2OfferingId,
                courseCode: "MATH101",
                academicLevelNumber: 1
            ));
        }

        // Course 3 sections: Tuesday 8-10, 10-12, 12-14
        for (int i = 0; i < 3; i++)
        {
            availableOptions.Add(new ActivityGroupOption(
                id: Guid.NewGuid(),
                name: $"PHYS101-G{i + 1}",
                meetings:
                [
                    new Meeting
                    {
                        DayOfWeek = DayOfWeek.Tuesday,
                        StartTime = new TimeOnly(8 + i * 2, 0),
                        EndTime = new TimeOnly(10 + i * 2, 0)
                    }
                ],
                activityType: ActivityType.Lecture,
                courseOfferingId: course3OfferingId,
                courseCode: "PHYS101",
                academicLevelNumber: 1
            ));
        }

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(1, "", ""),
            selectedCourses: [course1, course2, course3],
            availableOptions: availableOptions,
            preferences: new SchedulePreferenceProfile()
        );

        // Cap at 5 combinations
        var options = new SolverOptions
        {
            MaxCombinations = 5,
            TimeoutMilliseconds = 10_000
        };

        var solver = new ScheduleSolver(options);

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsCombinationsCapExceeded);
        Assert.True(result.TotalCombinationsEvaluated >= 5);
        Assert.NotEmpty(result.ValidSchedules);
    }

    [Fact]
    public void GenerateSchedules_WhenCancellationRequested_TruncatesEarlyAndSetsCapFlag()
    {
        // Arrange
        var course1OfferingId = Guid.NewGuid();
        var course1 = new SelectedCourseRequirement(
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        var group = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "CS101-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(10, 0)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            academicLevelNumber: 1
        );

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(1, "", ""),
            selectedCourses: [course1],
            availableOptions: [group],
            preferences: new SchedulePreferenceProfile()
        );

        var solver = new ScheduleSolver();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act
        var result = solver.GenerateSchedules(snapshot, cts.Token);

        // Assert
        Assert.True(result.IsCombinationsCapExceeded);
    }

    [Fact]
    public void GenerateSchedules_WhenUnderCap_ReturnsSuccessWithCapFlagFalse()
    {
        // Arrange
        var course1OfferingId = Guid.NewGuid();
        var course1 = new SelectedCourseRequirement(
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        var group = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "CS101-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(10, 0)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            academicLevelNumber: 1
        );

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(1, "", ""),
            selectedCourses: [course1],
            availableOptions: [group],
            preferences: new SchedulePreferenceProfile()
        );

        var solver = new ScheduleSolver(new SolverOptions { MaxCombinations = 50_000 });

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsCombinationsCapExceeded);
        Assert.Single(result.ValidSchedules);
    }
}
