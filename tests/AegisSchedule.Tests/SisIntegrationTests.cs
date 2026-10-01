using AegisSchedule.Api.Controllers;
using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Integrations;
using AegisSchedule.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisSchedule.Tests;

public class SisIntegrationTests
{
    private static UniSchedulingDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<UniSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new UniSchedulingDbContext(options);
    }

    private static async Task<(University uni, AcademicLevel lvl1, AcademicLevel lvl2, Term term)> SeedBaseCatalogAsync(UniSchedulingDbContext context)
    {
        var uni = new University { Id = Guid.NewGuid(), Name = "Minia National University" };
        var lvl1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var lvl2 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 2 };
        var term = new Term { Id = Guid.NewGuid(), Semester = Semester.Fall, Year = 2026, IsCurrent = true };

        await context.Universities.AddAsync(uni);
        await context.AcademicLevels.AddRangeAsync(lvl1, lvl2);
        await context.Terms.AddAsync(term);
        await context.SaveChangesAsync();

        return (uni, lvl1, lvl2, term);
    }

    private static SisBatchSyncRequest CreateSampleValidBatchRequest(Guid? universityId = null)
    {
        return new SisBatchSyncRequest
        {
            UniversityId = universityId,
            Mode = SisSyncMode.Upsert,
            Term = new SisTermSyncDto
            {
                Semester = Semester.Fall,
                Year = 2026,
                SetAsCurrent = true
            },
            Courses =
            [
                new SisCourseSyncDto
                {
                    Code = "CS101",
                    Name = "Intro to Computer Science",
                    AcademicLevelNumber = 1,
                    ExternalCourseId = "SIS-CS-101",
                    Activities =
                    [
                        new SisActivitySyncDto
                        {
                            Type = ActivityType.Lecture,
                            Groups =
                            [
                                new SisActivityGroupSyncDto
                                {
                                    Name = "LEC-01",
                                    ExternalSectionId = "CRN-101",
                                    Meetings =
                                    [
                                        new SisMeetingSyncDto
                                        {
                                            DayOfWeek = DayOfWeek.Sunday,
                                            StartTime = new TimeOnly(8, 30),
                                            EndTime = new TimeOnly(10, 0),
                                            Room = "Hall 1"
                                        }
                                    ]
                                }
                            ]
                        },
                        new SisActivitySyncDto
                        {
                            Type = ActivityType.Lab,
                            Groups =
                            [
                                new SisActivityGroupSyncDto
                                {
                                    Name = "LAB-01",
                                    ExternalSectionId = "CRN-102",
                                    Meetings =
                                    [
                                        new SisMeetingSyncDto
                                        {
                                            DayOfWeek = DayOfWeek.Tuesday,
                                            StartTime = new TimeOnly(10, 30),
                                            EndTime = new TimeOnly(12, 30),
                                            Room = "Lab A"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new SisCourseSyncDto
                {
                    Code = "MATH101",
                    Name = "Calculus I",
                    AcademicLevelNumber = 1,
                    ExternalCourseId = "SIS-MATH-101",
                    Activities =
                    [
                        new SisActivitySyncDto
                        {
                            Type = ActivityType.Lecture,
                            Groups =
                            [
                                new SisActivityGroupSyncDto
                                {
                                    Name = "LEC-01",
                                    ExternalSectionId = "CRN-201",
                                    Meetings =
                                    [
                                        new SisMeetingSyncDto
                                        {
                                            DayOfWeek = DayOfWeek.Monday,
                                            StartTime = new TimeOnly(9, 0),
                                            EndTime = new TimeOnly(11, 0),
                                            Room = "Hall 2"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    // =========================================================================
    // 1. Validator Tests
    // =========================================================================

    [Fact]
    public void SisBatchValidator_ValidPayload_ReturnsValid()
    {
        var request = CreateSampleValidBatchRequest();
        var result = SisBatchValidator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void SisBatchValidator_InvalidTimeRange_ReturnsError()
    {
        var request = CreateSampleValidBatchRequest();
        // Set invalid meeting time: StartTime >= EndTime
        request.Courses[0].Activities[0].Groups[0].Meetings[0].StartTime = new TimeOnly(12, 0);
        request.Courses[0].Activities[0].Groups[0].Meetings[0].EndTime = new TimeOnly(10, 0);

        var result = SisBatchValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_TIME_RANGE");
    }

    [Fact]
    public void SisBatchValidator_IntraGroupOverlap_ReturnsError()
    {
        var request = CreateSampleValidBatchRequest();
        // Add overlapping meeting in same group on Sunday
        request.Courses[0].Activities[0].Groups[0].Meetings.Add(new SisMeetingSyncDto
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(9, 0), // Overlaps with 08:30 - 10:00
            EndTime = new TimeOnly(10, 30),
            Room = "Hall 1"
        });

        var result = SisBatchValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INTRA_GROUP_OVERLAP");
    }

    [Fact]
    public void SisBatchValidator_DuplicateGroupNames_ReturnsError()
    {
        var request = CreateSampleValidBatchRequest();
        // Add duplicate group name in same activity
        request.Courses[0].Activities[0].Groups.Add(new SisActivityGroupSyncDto
        {
            Name = "lec-01", // Duplicate case-insensitive
            Meetings = []
        });

        var result = SisBatchValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "DUPLICATE_GROUP_NAME");
    }

    // =========================================================================
    // 2. Full Sync Ingestion Test
    // =========================================================================

    [Fact]
    public async Task SisBatchSync_FullSync_PersistsEntireCatalogGraph()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, _, _, _) = await SeedBaseCatalogAsync(context);

        var service = new SisImportService(context, NullLogger<SisImportService>.Instance);
        var request = CreateSampleValidBatchRequest(uni.Id);

        var result = await service.SyncBatchAsync(request);

        Assert.True(result.Success);
        Assert.Equal(2, result.Metrics.CoursesCreated);
        Assert.Equal(2, result.Metrics.OfferingsCreated);
        Assert.Equal(3, result.Metrics.ActivitiesCreated);
        Assert.Equal(3, result.Metrics.GroupsCreated);
        Assert.Equal(3, result.Metrics.MeetingsCreated);

        // Verify entities in database
        Assert.Equal(2, await context.Courses.CountAsync());
        Assert.Equal(2, await context.CourseOfferings.CountAsync());
        Assert.Equal(3, await context.Activities.CountAsync());
        Assert.Equal(3, await context.ActivityGroups.CountAsync());
        Assert.Equal(3, await context.Meetings.CountAsync());

        var cs101 = await context.Courses.FirstOrDefaultAsync(c => c.Code == "CS101");
        Assert.NotNull(cs101);
        Assert.Equal("Intro to Computer Science", cs101.Name);
    }

    // =========================================================================
    // 3. Idempotency Test
    // =========================================================================

    [Fact]
    public async Task SisBatchSync_Idempotency_RunningTwiceYieldsZeroDuplicates()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, _, _, _) = await SeedBaseCatalogAsync(context);

        var service = new SisImportService(context, NullLogger<SisImportService>.Instance);
        var request = CreateSampleValidBatchRequest(uni.Id);

        // First Run
        var result1 = await service.SyncBatchAsync(request);
        Assert.True(result1.Success);

        // Second Run with identical payload
        var result2 = await service.SyncBatchAsync(request);

        Assert.True(result2.Success);
        Assert.Empty(result2.Errors);
        Assert.Equal(0, result2.Metrics.CoursesCreated);
        Assert.Equal(0, result2.Metrics.CoursesUpdated);
        Assert.Equal(0, result2.Metrics.OfferingsCreated);
        Assert.Equal(0, result2.Metrics.ActivitiesCreated);
        Assert.Equal(0, result2.Metrics.GroupsCreated);
        Assert.Equal(0, result2.Metrics.MeetingsCreated);
        Assert.Equal(0, result2.Metrics.EntitiesRemoved);

        // Check database total counts remained identical
        Assert.Equal(2, await context.Courses.CountAsync());
        Assert.Equal(2, await context.CourseOfferings.CountAsync());
        Assert.Equal(3, await context.Activities.CountAsync());
        Assert.Equal(3, await context.ActivityGroups.CountAsync());
        Assert.Equal(3, await context.Meetings.CountAsync());
    }

    // =========================================================================
    // 4. Atomic Rollback Test
    // =========================================================================

    [Fact]
    public async Task SisBatchSync_AtomicRollback_MalformedMeetingRollsBackEntireCatalog()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, _, _, _) = await SeedBaseCatalogAsync(context);

        var service = new SisImportService(context, NullLogger<SisImportService>.Instance);
        var request = CreateSampleValidBatchRequest(uni.Id);

        // Corrupt the second course meeting time range
        request.Courses[1].Activities[0].Groups[0].Meetings[0].StartTime = new TimeOnly(15, 0);
        request.Courses[1].Activities[0].Groups[0].Meetings[0].EndTime = new TimeOnly(13, 0);

        var result = await service.SyncBatchAsync(request);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_TIME_RANGE");

        // Verify zero entities were created in the database
        Assert.Equal(0, await context.Courses.CountAsync());
        Assert.Equal(0, await context.CourseOfferings.CountAsync());
        Assert.Equal(0, await context.Activities.CountAsync());
        Assert.Equal(0, await context.ActivityGroups.CountAsync());
        Assert.Equal(0, await context.Meetings.CountAsync());
    }

    // =========================================================================
    // 5. Updates Test
    // =========================================================================

    [Fact]
    public async Task SisBatchSync_Updates_ModifiesCourseAndMeetingsCleanly()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, _, _, _) = await SeedBaseCatalogAsync(context);

        var service = new SisImportService(context, NullLogger<SisImportService>.Instance);
        var request1 = CreateSampleValidBatchRequest(uni.Id);
        await service.SyncBatchAsync(request1);

        // Second request modifies course name and updates meeting slot time
        var request2 = CreateSampleValidBatchRequest(uni.Id);
        request2.Courses[0].Name = "Introduction to Computer Science (Updated)";
        request2.Courses[0].Activities[0].Groups[0].Meetings[0].StartTime = new TimeOnly(9, 0);
        request2.Courses[0].Activities[0].Groups[0].Meetings[0].EndTime = new TimeOnly(10, 30);
        request2.Courses[0].Activities[0].Groups[0].Meetings[0].Room = "Hall 101";

        var result2 = await service.SyncBatchAsync(request2);

        Assert.True(result2.Success);
        Assert.Equal(1, result2.Metrics.CoursesUpdated);
        Assert.Equal(0, result2.Metrics.CoursesCreated);

        // Verify updated values in database
        var cs101 = await context.Courses.FirstAsync(c => c.Code == "CS101");
        Assert.Equal("Introduction to Computer Science (Updated)", cs101.Name);

        var updatedMeeting = await context.Meetings.FirstAsync(m => m.Room == "Hall 101");
        Assert.Equal(new TimeOnly(9, 0), updatedMeeting.StartTime);
        Assert.Equal(new TimeOnly(10, 30), updatedMeeting.EndTime);
    }

    // =========================================================================
    // 6. FullTermReconcile Test
    // =========================================================================

    [Fact]
    public async Task SisBatchSync_FullTermReconcile_PurgesOmittedOfferings()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, _, _, _) = await SeedBaseCatalogAsync(context);

        var service = new SisImportService(context, NullLogger<SisImportService>.Instance);
        var request1 = CreateSampleValidBatchRequest(uni.Id);
        await service.SyncBatchAsync(request1);
        Assert.Equal(2, await context.CourseOfferings.CountAsync());

        // In second sync with FullTermReconcile mode, omit MATH101
        var request2 = CreateSampleValidBatchRequest(uni.Id);
        request2.Mode = SisSyncMode.FullTermReconcile;
        request2.Courses.RemoveAll(c => c.Code == "MATH101");

        var result2 = await service.SyncBatchAsync(request2);

        Assert.True(result2.Success);
        Assert.Equal(1, await context.CourseOfferings.CountAsync());

        var remainingOffering = await context.CourseOfferings.FirstAsync();
        var remainingCourse = await context.Courses.FirstAsync(c => c.Id == remainingOffering.CourseId);
        Assert.Equal("CS101", remainingCourse.Code);
    }

    // =========================================================================
    // 7. AdminSisSyncController HTTP Status Codes
    // =========================================================================

    [Fact]
    public async Task AdminSisSyncController_ReturnsExpectedStatusCodes()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, _, _, _) = await SeedBaseCatalogAsync(context);

        var service = new SisImportService(context, NullLogger<SisImportService>.Instance);
        var controller = new AdminSisSyncController(service);

        // 1. Success returns 200 OK
        var validReq = CreateSampleValidBatchRequest(uni.Id);
        var okResult = await controller.SyncBatch(validReq);
        var okObject = Assert.IsType<OkObjectResult>(okResult.Result);
        var syncResult = Assert.IsType<SisBatchSyncResult>(okObject.Value);
        Assert.True(syncResult.Success);

        // 2. Validation error returns 422 UnprocessableEntity
        var invalidReq = CreateSampleValidBatchRequest(uni.Id);
        invalidReq.Courses[0].Activities[0].Groups[0].Meetings[0].StartTime = new TimeOnly(12, 0);
        invalidReq.Courses[0].Activities[0].Groups[0].Meetings[0].EndTime = new TimeOnly(10, 0);
        var unprocessableResult = await controller.SyncBatch(invalidReq);
        Assert.IsType<UnprocessableEntityObjectResult>(unprocessableResult.Result);

        // 3. Unknown Academic Level returns 400 BadRequest
        var badLevelReq = CreateSampleValidBatchRequest(uni.Id);
        badLevelReq.Courses[0].AcademicLevelNumber = 999;
        var badRequestResult = await controller.SyncBatch(badLevelReq);
        Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
    }
}
