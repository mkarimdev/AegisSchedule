using System.Text.Json;
using AegisSchedule.Api.Controllers;
using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using AegisSchedule.Api.Solver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AegisSchedule.Tests;

public class ScheduleControllerTests
{
    private static UniSchedulingDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<UniSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new UniSchedulingDbContext(options);
    }

    [Fact]
    public async Task Generate_WhenRequestIsEmpty_ReturnsOkWithEmptySchedules()
    {
        // Arrange
        using var context = CreateInMemoryContext(nameof(Generate_WhenRequestIsEmpty_ReturnsOkWithEmptySchedules));
        var solver = new ScheduleSolver();
        var controller = new SchedulesController(context, solver);

        var request = new GenerateScheduleRequest
        {
            AcademicLevelNumber = 1,
            SelectedCourseOfferingIds = []
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GenerateScheduleResponse>(okResult.Value);
        Assert.True(response.IsSuccess);
        Assert.Empty(response.Schedules);
        Assert.Equal(0, response.TotalCombinationsEvaluated);
    }

    [Fact]
    public async Task Generate_WhenTwoCoursesHaveNoOverlap_ReturnsOkWithComputedSchedulesAndCorrectDtoMapping()
    {
        // Arrange
        var dbName = nameof(Generate_WhenTwoCoursesHaveNoOverlap_ReturnsOkWithComputedSchedulesAndCorrectDtoMapping);
        using var context = CreateInMemoryContext(dbName);

        var uni = new University { Id = Guid.NewGuid(), Name = "Minia National University" };
        var level1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var term = new Term { Id = Guid.NewGuid(), Semester = Semester.Fall, Year = 2026, IsCurrent = true };

        var courseCs = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "CS101", Name = "Intro to CS" };
        var courseMath = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "MATH101", Name = "Calculus" };

        var offeringCs = new CourseOffering { Id = Guid.NewGuid(), CourseId = courseCs.Id, TermId = term.Id, UniversityId = uni.Id };
        var offeringMath = new CourseOffering { Id = Guid.NewGuid(), CourseId = courseMath.Id, TermId = term.Id, UniversityId = uni.Id };

        var actCs = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs.Id, Type = ActivityType.Lecture };
        var actMath = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringMath.Id, Type = ActivityType.Lecture };

        var groupCs1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = actCs.Id, Name = "CS-G1" };
        var groupMath1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = actMath.Id, Name = "MATH-G1" };

        var meetingCs1 = new Meeting
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupCs1.Id,
            DayOfWeek = DayOfWeek.Saturday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 30),
            Room = "Hall 101"
        };

        var meetingMath1 = new Meeting
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupMath1.Id,
            DayOfWeek = DayOfWeek.Saturday,
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(12, 30),
            Room = "Hall 102"
        };

        await context.Universities.AddAsync(uni);
        await context.AcademicLevels.AddAsync(level1);
        await context.Terms.AddAsync(term);
        await context.Courses.AddRangeAsync(courseCs, courseMath);
        await context.CourseOfferings.AddRangeAsync(offeringCs, offeringMath);
        await context.Activities.AddRangeAsync(actCs, actMath);
        await context.ActivityGroups.AddRangeAsync(groupCs1, groupMath1);
        await context.Meetings.AddRangeAsync(meetingCs1, meetingMath1);
        await context.SaveChangesAsync();

        var solver = new ScheduleSolver();
        var controller = new SchedulesController(context, solver);

        var request = new GenerateScheduleRequest
        {
            AcademicLevelNumber = 1,
            PrimaryLectureGroupName = null,
            PrimaryLabSectionName = null,
            SelectedCourseOfferingIds = [offeringCs.Id, offeringMath.Id]
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GenerateScheduleResponse>(okResult.Value);

        Assert.True(response.IsSuccess);
        Assert.Single(response.Schedules);
        Assert.True(response.TotalCombinationsEvaluated >= 1);

        var schedule = response.Schedules[0];
        Assert.NotEqual(Guid.Empty, schedule.ScheduleId);
        Assert.Equal(2, schedule.SelectedGroups.Count);

        var csGroupDto = schedule.SelectedGroups.FirstOrDefault(g => g.GroupId == groupCs1.Id);
        Assert.NotNull(csGroupDto);
        Assert.Equal("CS-G1", csGroupDto.GroupName);
        Assert.Equal("CS101", csGroupDto.CourseCode);
        Assert.Equal("Lecture", csGroupDto.ActivityType);
        Assert.Single(csGroupDto.Meetings);
        Assert.Equal(DayOfWeek.Saturday, csGroupDto.Meetings[0].DayOfWeek);
        Assert.Equal(new TimeOnly(8, 30), csGroupDto.Meetings[0].StartTime);
        Assert.Equal(new TimeOnly(10, 30), csGroupDto.Meetings[0].EndTime);
        Assert.Equal("Hall 101", csGroupDto.Meetings[0].Room);

        var mathGroupDto = schedule.SelectedGroups.FirstOrDefault(g => g.GroupId == groupMath1.Id);
        Assert.NotNull(mathGroupDto);
        Assert.Equal("MATH-G1", mathGroupDto.GroupName);
        Assert.Equal("MATH101", mathGroupDto.CourseCode);
        Assert.Equal("Lecture", mathGroupDto.ActivityType);
        Assert.Single(mathGroupDto.Meetings);
        Assert.Equal("Hall 102", mathGroupDto.Meetings[0].Room);
    }

    [Fact]
    public async Task Generate_WhenAllMeetingsCollide_ReturnsOkWithZeroValidSchedules()
    {
        // Arrange
        var dbName = nameof(Generate_WhenAllMeetingsCollide_ReturnsOkWithZeroValidSchedules);
        using var context = CreateInMemoryContext(dbName);

        var uni = new University { Id = Guid.NewGuid(), Name = "Minia National University" };
        var level1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var term = new Term { Id = Guid.NewGuid(), Semester = Semester.Fall, Year = 2026, IsCurrent = true };

        var courseA = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "CS101", Name = "Course A" };
        var courseB = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "CS102", Name = "Course B" };

        var offeringA = new CourseOffering { Id = Guid.NewGuid(), CourseId = courseA.Id, TermId = term.Id, UniversityId = uni.Id };
        var offeringB = new CourseOffering { Id = Guid.NewGuid(), CourseId = courseB.Id, TermId = term.Id, UniversityId = uni.Id };

        var actA = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringA.Id, Type = ActivityType.Lecture };
        var actB = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringB.Id, Type = ActivityType.Lecture };

        var groupA = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = actA.Id, Name = "Group A" };
        var groupB = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = actB.Id, Name = "Group B" };

        // Overlapping times on the same day
        var meetingA = new Meeting
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupA.Id,
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        };
        var meetingB = new Meeting
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupB.Id,
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0)
        };

        await context.Universities.AddAsync(uni);
        await context.AcademicLevels.AddAsync(level1);
        await context.Terms.AddAsync(term);
        await context.Courses.AddRangeAsync(courseA, courseB);
        await context.CourseOfferings.AddRangeAsync(offeringA, offeringB);
        await context.Activities.AddRangeAsync(actA, actB);
        await context.ActivityGroups.AddRangeAsync(groupA, groupB);
        await context.Meetings.AddRangeAsync(meetingA, meetingB);
        await context.SaveChangesAsync();

        var solver = new ScheduleSolver();
        var controller = new SchedulesController(context, solver);

        var request = new GenerateScheduleRequest
        {
            AcademicLevelNumber = 1,
            SelectedCourseOfferingIds = [offeringA.Id, offeringB.Id]
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GenerateScheduleResponse>(okResult.Value);
        Assert.True(response.IsSuccess);
        Assert.Empty(response.Schedules);
    }

    [Fact]
    public async Task Generate_WithCohortLock_EnforcesPrimaryGroupForSameLevel()
    {
        // Arrange
        var dbName = nameof(Generate_WithCohortLock_EnforcesPrimaryGroupForSameLevel);
        using var context = CreateInMemoryContext(dbName);

        var uni = new University { Id = Guid.NewGuid(), Name = "Minia National University" };
        var level1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var term = new Term { Id = Guid.NewGuid(), Semester = Semester.Fall, Year = 2026, IsCurrent = true };

        var course = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "CS101", Name = "Intro" };
        var offering = new CourseOffering { Id = Guid.NewGuid(), CourseId = course.Id, TermId = term.Id, UniversityId = uni.Id };
        var act = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offering.Id, Type = ActivityType.Lecture };

        var group1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = act.Id, Name = "Cohort 1" };
        var group2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = act.Id, Name = "Cohort 2" };

        var meeting1 = new Meeting { Id = Guid.NewGuid(), ActivityGroupId = group1.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30) };
        var meeting2 = new Meeting { Id = Guid.NewGuid(), ActivityGroupId = group2.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) };

        await context.Universities.AddAsync(uni);
        await context.AcademicLevels.AddAsync(level1);
        await context.Terms.AddAsync(term);
        await context.Courses.AddAsync(course);
        await context.CourseOfferings.AddAsync(offering);
        await context.Activities.AddAsync(act);
        await context.ActivityGroups.AddRangeAsync(group1, group2);
        await context.Meetings.AddRangeAsync(meeting1, meeting2);
        await context.SaveChangesAsync();

        var solver = new ScheduleSolver();
        var controller = new SchedulesController(context, solver);

        var request = new GenerateScheduleRequest
        {
            AcademicLevelNumber = 1,
            PrimaryLectureGroupName = group1.Name, // Locked to group1
            SelectedCourseOfferingIds = [offering.Id]
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GenerateScheduleResponse>(okResult.Value);
        Assert.True(response.IsSuccess);
        Assert.Single(response.Schedules);
        Assert.Equal(group1.Id, response.Schedules[0].SelectedGroups[0].GroupId);
    }

    [Fact]
    public async Task Generate_WithCrossLevelCourse_PermitsOptionsFromOtherLevel()
    {
        // Arrange
        var dbName = nameof(Generate_WithCrossLevelCourse_PermitsOptionsFromOtherLevel);
        using var context = CreateInMemoryContext(dbName);

        var uni = new University { Id = Guid.NewGuid(), Name = "Minia National University" };
        var level1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var level2 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 2 };
        var term = new Term { Id = Guid.NewGuid(), Semester = Semester.Fall, Year = 2026, IsCurrent = true };

        // Student is Level 1, but takes a Level 2 course
        var courseL2 = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level2.Id, Code = "CS201", Name = "Databases" };
        var offeringL2 = new CourseOffering { Id = Guid.NewGuid(), CourseId = courseL2.Id, TermId = term.Id, UniversityId = uni.Id };
        var actL2 = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringL2.Id, Type = ActivityType.Lecture };

        var groupL2A = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = actL2.Id, Name = "L2 Group A" };
        var groupL2B = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = actL2.Id, Name = "L2 Group B" };

        var meetingL2A = new Meeting { Id = Guid.NewGuid(), ActivityGroupId = groupL2A.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30) };
        var meetingL2B = new Meeting { Id = Guid.NewGuid(), ActivityGroupId = groupL2B.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) };

        await context.Universities.AddAsync(uni);
        await context.AcademicLevels.AddRangeAsync(level1, level2);
        await context.Terms.AddAsync(term);
        await context.Courses.AddAsync(courseL2);
        await context.CourseOfferings.AddAsync(offeringL2);
        await context.Activities.AddAsync(actL2);
        await context.ActivityGroups.AddRangeAsync(groupL2A, groupL2B);
        await context.Meetings.AddRangeAsync(meetingL2A, meetingL2B);
        await context.SaveChangesAsync();

        var solver = new ScheduleSolver();
        var controller = new SchedulesController(context, solver);

        // Student has Level 1 primary group name (which does not match groupL2A or groupL2B)
        var request = new GenerateScheduleRequest
        {
            AcademicLevelNumber = 1,
            PrimaryLectureGroupName = "NonExistentLevel1Group",
            SelectedCourseOfferingIds = [offeringL2.Id]
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GenerateScheduleResponse>(okResult.Value);
        Assert.True(response.IsSuccess);
        // Both groups should be valid candidates because the course is cross-level (Level 2 != Level 1)
        Assert.Equal(2, response.Schedules.Count);
    }

    [Fact]
    public void Dto_SerializationAndDeserialization_WorksCorrectly()
    {
        // Arrange
        var request = new GenerateScheduleRequest
        {
            StudentId = Guid.NewGuid(),
            AcademicLevelNumber = 2,
            PrimaryLectureGroupName = "Lecture Group 1",
            PrimaryLabSectionName = "Lab Section 18",
            SelectedCourseOfferingIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(request, options);
        var deserialized = JsonSerializer.Deserialize<GenerateScheduleRequest>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(request.StudentId, deserialized.StudentId);
        Assert.Equal(request.AcademicLevelNumber, deserialized.AcademicLevelNumber);
        Assert.Equal(request.PrimaryLectureGroupName, deserialized.PrimaryLectureGroupName);
        Assert.Equal(request.PrimaryLabSectionName, deserialized.PrimaryLabSectionName);
        Assert.Equal(request.SelectedCourseOfferingIds.Count, deserialized.SelectedCourseOfferingIds.Count);

        // Also test response serialization
        var response = new GenerateScheduleResponse
        {
            IsSuccess = true,
            TotalCombinationsEvaluated = 42,
            ErrorMessage = null,
            Schedules =
            [
                new ScheduleDto
                {
                    ScheduleId = Guid.NewGuid(),
                    SelectedGroups =
                    [
                        new SelectedGroupDto
                        {
                            GroupId = Guid.NewGuid(),
                            GroupName = "Group 1",
                            CourseCode = "CS101",
                            ActivityType = "Lecture",
                            Meetings =
                            [
                                new MeetingDto
                                {
                                    DayOfWeek = DayOfWeek.Monday,
                                    StartTime = new TimeOnly(8, 30),
                                    EndTime = new TimeOnly(10, 30),
                                    Room = "Hall 1"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var responseJson = JsonSerializer.Serialize(response, options);
        var deserializedResponse = JsonSerializer.Deserialize<GenerateScheduleResponse>(responseJson, options);

        Assert.NotNull(deserializedResponse);
        Assert.True(deserializedResponse.IsSuccess);
        Assert.Equal(42, deserializedResponse.TotalCombinationsEvaluated);
        Assert.Single(deserializedResponse.Schedules);
        Assert.Equal("Hall 1", deserializedResponse.Schedules[0].SelectedGroups[0].Meetings[0].Room);
    }
}
