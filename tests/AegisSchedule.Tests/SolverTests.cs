using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Solver;

namespace AegisSchedule.Tests;

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
                primaryLectureGroupName: "CS201-G1"
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
                primaryLectureGroupName: "Group 1", // primary group for Level 3 does not lock Level 2
                primaryLabSectionName: "Section 1"
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

    [Fact]
    public void GenerateSchedules_WithDualCohortLock_LocksBothLectureAndLabForSameLevel()
    {
        // Arrange: Level 1 course CS101 with 2 Lectures and 2 Labs
        var courseOfferingId = Guid.NewGuid();
        var course = new SelectedCourseRequirement(
            courseOfferingId: courseOfferingId,
            courseCode: "CS101",
            activityRequirements:
            [
                new ActivityRequirement(ActivityType.Lecture),
                new ActivityRequirement(ActivityType.Lab)
            ],
            academicLevelNumber: 1
        );

        var lec1 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 1", [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lecture, courseOfferingId, "CS101", 1);
        var lec2 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 2", [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }], ActivityType.Lecture, courseOfferingId, "CS101", 1);
        var lab1 = new ActivityGroupOption(Guid.NewGuid(), "Lab Section 1", [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lab, courseOfferingId, "CS101", 1);
        var lab2 = new ActivityGroupOption(Guid.NewGuid(), "Lab Section 2", [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }], ActivityType.Lab, courseOfferingId, "CS101", 1);

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(
                academicLevelNumber: 1,
                primaryLectureGroupName: "Lecture Group 1",
                primaryLabSectionName: "Lab Section 2"
            ),
            selectedCourses: [course],
            availableOptions: [lec1, lec2, lab1, lab2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert: Exactly 1 schedule with lec1 and lab2
        Assert.True(result.IsSuccess);
        Assert.Single(result.ValidSchedules);
        var selected = result.ValidSchedules[0].SelectedGroups;
        Assert.Contains(selected, g => g.Id == lec1.Id);
        Assert.Contains(selected, g => g.Id == lab2.Id);
        Assert.DoesNotContain(selected, g => g.Id == lec2.Id);
        Assert.DoesNotContain(selected, g => g.Id == lab1.Id);
    }

    [Fact]
    public void GenerateSchedules_WithMultipleSameLevelCourses_LocksAllLecturesByName()
    {
        // Arrange: CS101 and MATH101 both at Level 1, each with Lecture Group 1 and 2 (distinct Guids)
        var cs101Id = Guid.NewGuid();
        var math101Id = Guid.NewGuid();

        var csCourse = new SelectedCourseRequirement(cs101Id, "CS101", [new ActivityRequirement(ActivityType.Lecture)], 1);
        var mathCourse = new SelectedCourseRequirement(math101Id, "MATH101", [new ActivityRequirement(ActivityType.Lecture)], 1);

        var csLec1 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 1", [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lecture, cs101Id, "CS101", 1);
        var csLec2 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 2", [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }], ActivityType.Lecture, cs101Id, "CS101", 1);

        var mathLec1 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 1", [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lecture, math101Id, "MATH101", 1);
        var mathLec2 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 2", [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }], ActivityType.Lecture, math101Id, "MATH101", 1);

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(
                academicLevelNumber: 1,
                primaryLectureGroupName: "Lecture Group 1"
            ),
            selectedCourses: [csCourse, mathCourse],
            availableOptions: [csLec1, csLec2, mathLec1, mathLec2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert: Single schedule where both courses selected their respective Lecture Group 1
        Assert.True(result.IsSuccess);
        Assert.Single(result.ValidSchedules);
        var schedule = result.ValidSchedules[0];
        Assert.Contains(schedule.SelectedGroups, g => g.Id == csLec1.Id);
        Assert.Contains(schedule.SelectedGroups, g => g.Id == mathLec1.Id);
    }

    [Fact]
    public void GenerateSchedules_WhenOnlyLectureLocked_KeepsLabsFlexible()
    {
        // Arrange: CS101 with Lecture Group 1 and 2 labs
        var cs101Id = Guid.NewGuid();
        var csCourse = new SelectedCourseRequirement(cs101Id, "CS101",
        [
            new ActivityRequirement(ActivityType.Lecture),
            new ActivityRequirement(ActivityType.Lab)
        ], 1);

        var lec1 = new ActivityGroupOption(Guid.NewGuid(), "Lecture Group 1", [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lecture, cs101Id, "CS101", 1);
        var lab1 = new ActivityGroupOption(Guid.NewGuid(), "Lab Section 1", [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lab, cs101Id, "CS101", 1);
        var lab2 = new ActivityGroupOption(Guid.NewGuid(), "Lab Section 2", [new Meeting { DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }], ActivityType.Lab, cs101Id, "CS101", 1);

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(
                academicLevelNumber: 1,
                primaryLectureGroupName: "Lecture Group 1",
                primaryLabSectionName: null // flexible lab
            ),
            selectedCourses: [csCourse],
            availableOptions: [lec1, lab1, lab2]
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert: 2 schedules generated, both having lec1, one with lab1 and one with lab2
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidSchedules.Count);
        Assert.All(result.ValidSchedules, s => Assert.Contains(s.SelectedGroups, g => g.Id == lec1.Id));
        Assert.Contains(result.ValidSchedules, s => s.SelectedGroups.Any(g => g.Id == lab1.Id));
        Assert.Contains(result.ValidSchedules, s => s.SelectedGroups.Any(g => g.Id == lab2.Id));
    }

    [Fact]
    public void RankSchedules_MinimizeDays_PrioritizesScheduleWithFewerDays()
    {
        // Arrange
        var csId = Guid.NewGuid();
        var mathId = Guid.NewGuid();

        var csCourse = new SelectedCourseRequirement(csId, "CS101", [new ActivityRequirement(ActivityType.Lecture)], 1);
        var mathCourse = new SelectedCourseRequirement(mathId, "MATH101", [new ActivityRequirement(ActivityType.Lecture)], 1);

        // CS101 on Sunday
        var csLec = new ActivityGroupOption(
            Guid.NewGuid(), "CS101-L1",
            [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }],
            ActivityType.Lecture, csId, "CS101", 1);

        // MATH101 Option A on Sunday (1 total day: Sunday)
        var mathLecA = new ActivityGroupOption(
            Guid.NewGuid(), "MATH101-LA",
            [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }],
            ActivityType.Lecture, mathId, "MATH101", 1);

        // MATH101 Option B on Monday (2 total days: Sunday, Monday)
        var mathLecB = new ActivityGroupOption(
            Guid.NewGuid(), "MATH101-LB",
            [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }],
            ActivityType.Lecture, mathId, "MATH101", 1);

        var preferences = new SchedulePreferenceProfile(
            MinimizeDaysWeight: 5,
            MinimizeGapsWeight: 0,
            PreferredTimeBlock: TimeBlockPreference.None,
            PreferredTimeBlockWeight: 0);

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(academicLevelNumber: 1),
            selectedCourses: [csCourse, mathCourse],
            availableOptions: [csLec, mathLecA, mathLecB],
            preferences: preferences
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidSchedules.Count);

        var rank1 = result.ValidSchedules[0];
        var rank2 = result.ValidSchedules[1];

        Assert.Equal(1, rank1.Rank);
        Assert.Equal(2, rank2.Rank);

        // Option A (1 day) must score higher and be rank #1
        Assert.Equal(1, rank1.ScoreBreakdown.TotalDays);
        Assert.Equal(2, rank2.ScoreBreakdown.TotalDays);
        Assert.True(rank1.ScoreBreakdown.DaysScore > rank2.ScoreBreakdown.DaysScore);
        Assert.True(rank1.OverallScore > rank2.OverallScore);
        Assert.Contains(rank1.SelectedGroups, g => g.Id == mathLecA.Id);
        Assert.Contains(rank2.SelectedGroups, g => g.Id == mathLecB.Id);
    }

    [Fact]
    public void RankSchedules_MinimizeGaps_PrioritizesZeroGapOverTwoHourGap()
    {
        // Arrange
        var csId = Guid.NewGuid();
        var mathId = Guid.NewGuid();

        var csCourse = new SelectedCourseRequirement(csId, "CS101", [new ActivityRequirement(ActivityType.Lecture)], 1);
        var mathCourse = new SelectedCourseRequirement(mathId, "MATH101", [new ActivityRequirement(ActivityType.Lecture)], 1);

        // CS101 on Sunday 09:00 - 11:00
        var csLec = new ActivityGroupOption(
            Guid.NewGuid(), "CS101-L1",
            [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }],
            ActivityType.Lecture, csId, "CS101", 1);

        // Option A: Sunday 11:00 - 13:00 (Gap = 0 hours)
        var mathLecNoGap = new ActivityGroupOption(
            Guid.NewGuid(), "MATH101-L1",
            [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) }],
            ActivityType.Lecture, mathId, "MATH101", 1);

        // Option B: Sunday 13:00 - 15:00 (Gap = 2 hours between 11:00 and 13:00)
        var mathLecWithGap = new ActivityGroupOption(
            Guid.NewGuid(), "MATH101-L2",
            [new Meeting { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0) }],
            ActivityType.Lecture, mathId, "MATH101", 1);

        var preferences = new SchedulePreferenceProfile(
            MinimizeDaysWeight: 0,
            MinimizeGapsWeight: 5,
            PreferredTimeBlock: TimeBlockPreference.None,
            PreferredTimeBlockWeight: 0);

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(academicLevelNumber: 1),
            selectedCourses: [csCourse, mathCourse],
            availableOptions: [csLec, mathLecNoGap, mathLecWithGap],
            preferences: preferences
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidSchedules.Count);

        var rank1 = result.ValidSchedules[0];
        var rank2 = result.ValidSchedules[1];

        Assert.Equal(1, rank1.Rank);
        Assert.Equal(2, rank2.Rank);

        // Option A (0 gap hours) must score higher and be rank #1
        Assert.Equal(0.0, rank1.ScoreBreakdown.TotalGapHours);
        Assert.Equal(2.0, rank2.ScoreBreakdown.TotalGapHours);
        Assert.True(rank1.ScoreBreakdown.GapsScore > rank2.ScoreBreakdown.GapsScore);
        Assert.True(rank1.OverallScore > rank2.OverallScore);
        Assert.Contains(rank1.SelectedGroups, g => g.Id == mathLecNoGap.Id);
        Assert.Contains(rank2.SelectedGroups, g => g.Id == mathLecWithGap.Id);
    }

    [Fact]
    public void RankSchedules_TimeBlockPreference_PrioritizesMorningClasses()
    {
        // Arrange
        var csId = Guid.NewGuid();

        var csCourse = new SelectedCourseRequirement(csId, "CS101", [new ActivityRequirement(ActivityType.Lecture)], 1);

        // Option A: Morning meeting 08:30 - 10:30 (Morning window: 08:00 - 12:30 -> 100% overlap)
        var morningLec = new ActivityGroupOption(
            Guid.NewGuid(), "CS101-Morning",
            [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30) }],
            ActivityType.Lecture, csId, "CS101", 1);

        // Option B: Afternoon meeting 13:00 - 15:00 (Morning window -> 0% overlap)
        var afternoonLec = new ActivityGroupOption(
            Guid.NewGuid(), "CS101-Afternoon",
            [new Meeting { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0) }],
            ActivityType.Lecture, csId, "CS101", 1);

        var preferences = new SchedulePreferenceProfile(
            MinimizeDaysWeight: 0,
            MinimizeGapsWeight: 0,
            PreferredTimeBlock: TimeBlockPreference.Morning,
            PreferredTimeBlockWeight: 5);

        var snapshot = new SchedulingInputSnapshot(
            studentConstraint: new StudentGroupConstraint(academicLevelNumber: 1),
            selectedCourses: [csCourse],
            availableOptions: [morningLec, afternoonLec],
            preferences: preferences
        );

        var solver = new ScheduleSolver();

        // Act
        var result = solver.GenerateSchedules(snapshot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ValidSchedules.Count);

        var rank1 = result.ValidSchedules[0];
        var rank2 = result.ValidSchedules[1];

        Assert.Equal(1, rank1.Rank);
        Assert.Equal(2, rank2.Rank);

        // Morning option must have 100% alignment and be ranked #1
        Assert.Equal(100.0, rank1.ScoreBreakdown.TimeBlockAlignmentPercentage);
        Assert.Equal(0.0, rank2.ScoreBreakdown.TimeBlockAlignmentPercentage);
        Assert.True(rank1.ScoreBreakdown.TimeBlockScore > rank2.ScoreBreakdown.TimeBlockScore);
        Assert.True(rank1.OverallScore > rank2.OverallScore);
        Assert.Contains(rank1.SelectedGroups, g => g.Id == morningLec.Id);
        Assert.Contains(rank2.SelectedGroups, g => g.Id == afternoonLec.Id);
    }
}

