using Api.Domain;
using Api.Solver;

namespace Api.Tests;

public class SolverTests
{
    [Fact]
    public void GenerateSchedules_WhenTwoCoursesHaveNoOverlap_ReturnsOneValidSchedule()
    {
        // Arrange
        var course1OfferingId = Guid.NewGuid();
        var course2OfferingId = Guid.NewGuid();

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

        // Course 1 Meeting: Sunday 08:30 - 10:30
        var group1 = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "CS101-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeOnly(8, 30),
                    EndTime = new TimeOnly(10, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            academicLevelNumber: 1
        );

        // Course 2 Meeting: Sunday 10:30 - 12:30 (Adjacent, no collision)
        var group2 = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "MATH101-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeOnly(10, 30),
                    EndTime = new TimeOnly(12, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: course2OfferingId,
            courseCode: "MATH101",
            academicLevelNumber: 1
        );

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(academicLevelNumber: 1),
            selectedCourses: [course1, course2],
            availableOptions: [group1, group2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.ValidSchedules);
        Assert.Equal(1, result.TotalCombinationsEvaluated);

        var schedule = result.ValidSchedules[0];
        Assert.Equal(2, schedule.SelectedGroups.Count);
        Assert.Contains(schedule.SelectedGroups, g => g.Id == group1.Id);
        Assert.Contains(schedule.SelectedGroups, g => g.Id == group2.Id);
    }

    [Fact]
    public void GenerateSchedules_WhenTwoCoursesHaveOverlappingMeetings_RejectsAndReturnsZeroValidSchedules()
    {
        // Arrange
        var course1OfferingId = Guid.NewGuid();
        var course2OfferingId = Guid.NewGuid();

        var course1 = new SelectedCourseRequirement(
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        var course2 = new SelectedCourseRequirement(
            courseOfferingId: course2OfferingId,
            courseCode: "PHYS101",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 1
        );

        // Both groups scheduled on Monday 09:00 - 11:00 (Direct collision)
        var group1 = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "CS101-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 0)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: course1OfferingId,
            courseCode: "CS101",
            academicLevelNumber: 1
        );

        var group2 = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "PHYS101-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 30),
                    EndTime = new TimeOnly(11, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: course2OfferingId,
            courseCode: "PHYS101",
            academicLevelNumber: 1
        );

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(academicLevelNumber: 1),
            selectedCourses: [course1, course2],
            availableOptions: [group1, group2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.ValidSchedules);
        Assert.True(result.TotalCombinationsEvaluated >= 1);
    }

    [Fact]
    public void GenerateSchedules_WhenSameLevelCourse_EnforcesAssignedPrimaryGroupFilter()
    {
        // Arrange: Student is Level 2, with assigned PrimaryGroupId = primaryGroupG1Id
        var primaryGroupG1Id = Guid.NewGuid();
        var otherGroupG2Id = Guid.NewGuid();
        var courseOfferingId = Guid.NewGuid();

        var course = new SelectedCourseRequirement(
            courseOfferingId: courseOfferingId,
            courseCode: "CS201",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 2
        );

        // Group 1 matches student's primary assigned group
        var optionG1 = new ActivityGroupOption(
            id: primaryGroupG1Id,
            name: "CS201-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeOnly(8, 30),
                    EndTime = new TimeOnly(10, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: courseOfferingId,
            courseCode: "CS201",
            academicLevelNumber: 2
        );

        // Group 2 is a different cohort group for the same course
        var optionG2 = new ActivityGroupOption(
            id: otherGroupG2Id,
            name: "CS201-G2",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeOnly(8, 30),
                    EndTime = new TimeOnly(10, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: courseOfferingId,
            courseCode: "CS201",
            academicLevelNumber: 2
        );

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(
                academicLevelNumber: 2,
                assignedPrimaryGroupId: primaryGroupG1Id
            ),
            selectedCourses: [course],
            availableOptions: [optionG1, optionG2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.ValidSchedules);
        Assert.Equal(primaryGroupG1Id, result.ValidSchedules[0].SelectedGroups[0].Id);
        Assert.DoesNotContain(result.ValidSchedules, s => s.SelectedGroups.Any(g => g.Id == otherGroupG2Id));
    }

    [Fact]
    public void GenerateSchedules_WhenCrossLevelCourse_EvaluatesAllAvailableGroups()
    {
        // Arrange: Student is Level 3, but enrolling in Level 2 course CS201 (carry-over / prerequisite)
        var courseOfferingId = Guid.NewGuid();

        var crossLevelCourse = new SelectedCourseRequirement(
            courseOfferingId: courseOfferingId,
            courseCode: "CS201",
            activityRequirements: [new ActivityRequirement(ActivityType.Lecture)],
            academicLevelNumber: 2 // Level 2 (Cross-level for a Level 3 student)
        );

        var optionG1 = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "CS201-G1",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeOnly(8, 30),
                    EndTime = new TimeOnly(10, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: courseOfferingId,
            courseCode: "CS201",
            academicLevelNumber: 2
        );

        var optionG2 = new ActivityGroupOption(
            id: Guid.NewGuid(),
            name: "CS201-G2",
            meetings:
            [
                new Meeting
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(8, 30),
                    EndTime = new TimeOnly(10, 30)
                }
            ],
            activityType: ActivityType.Lecture,
            courseOfferingId: courseOfferingId,
            courseCode: "CS201",
            academicLevelNumber: 2
        );

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(
                academicLevelNumber: 3,
                assignedPrimaryGroupId: Guid.NewGuid() // primary group for Level 3 does not lock Level 2
            ),
            selectedCourses: [crossLevelCourse],
            availableOptions: [optionG1, optionG2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert: Both groups should be evaluated and produce 2 valid schedules
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidSchedules.Count);
        Assert.Contains(result.ValidSchedules, s => s.SelectedGroups.Any(g => g.Id == optionG1.Id));
        Assert.Contains(result.ValidSchedules, s => s.SelectedGroups.Any(g => g.Id == optionG2.Id));
    }
}
