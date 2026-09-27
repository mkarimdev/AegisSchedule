using Api.Controllers;
using Api.Domain;
using Api.DTOs;
using Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests;

public class AdminControllerTests
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

    // =========================================================================
    // AdminCoursesController Tests
    // =========================================================================

    [Fact]
    public async Task Courses_CreateAndGet_ReturnsCreatedCourseAndFilters()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, lvl1, lvl2, _) = await SeedBaseCatalogAsync(context);

        var controller = new AdminCoursesController(context);

        // 1. Create CS101
        var createResult = await controller.CreateCourse(new CreateCourseRequest
        {
            UniversityId = uni.Id,
            AcademicLevelId = lvl1.Id,
            Code = "CS101",
            Name = "Intro to Programming"
        });

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdDto = Assert.IsType<AdminCourseDto>(created.Value);
        Assert.Equal("CS101", createdDto.Code);
        Assert.Equal(1, createdDto.AcademicLevelNumber);

        // 2. Create CS201 in Level 2
        await controller.CreateCourse(new CreateCourseRequest
        {
            UniversityId = uni.Id,
            AcademicLevelId = lvl2.Id,
            Code = "CS201",
            Name = "Data Structures"
        });

        // 3. List filtered by Level 1
        var listResult = await controller.GetCourses(academicLevel: 1, search: null);
        var okList = Assert.IsType<OkObjectResult>(listResult.Result);
        var courses = Assert.IsType<List<AdminCourseDto>>(okList.Value);
        Assert.Single(courses);
        Assert.Equal("CS101", courses[0].Code);

        // 4. Search query
        var searchResult = await controller.GetCourses(academicLevel: null, search: "Structures");
        var okSearch = Assert.IsType<OkObjectResult>(searchResult.Result);
        var searched = Assert.IsType<List<AdminCourseDto>>(okSearch.Value);
        Assert.Single(searched);
        Assert.Equal("CS201", searched[0].Code);
    }

    [Fact]
    public async Task Courses_CreateDuplicateCode_ReturnsBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, lvl1, _, _) = await SeedBaseCatalogAsync(context);

        var controller = new AdminCoursesController(context);

        await controller.CreateCourse(new CreateCourseRequest
        {
            UniversityId = uni.Id,
            AcademicLevelId = lvl1.Id,
            Code = "CS101",
            Name = "Intro to Programming"
        });

        var duplicateResult = await controller.CreateCourse(new CreateCourseRequest
        {
            UniversityId = uni.Id,
            AcademicLevelId = lvl1.Id,
            Code = "cs101", // case-insensitive duplicate check
            Name = "Duplicate Course"
        });

        Assert.IsType<BadRequestObjectResult>(duplicateResult.Result);
    }

    [Fact]
    public async Task Courses_Delete_ActiveOfferingGuard_WorksAsExpected()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, lvl1, _, term) = await SeedBaseCatalogAsync(context);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = uni.Id,
            AcademicLevelId = lvl1.Id,
            Code = "MATH101",
            Name = "Calculus"
        };
        var offering = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            TermId = term.Id,
            UniversityId = uni.Id
        };
        await context.Courses.AddAsync(course);
        await context.CourseOfferings.AddAsync(offering);
        await context.SaveChangesAsync();

        var controller = new AdminCoursesController(context);

        // Deletion without force should fail
        var guardedResult = await controller.DeleteCourse(course.Id, force: false);
        Assert.IsType<BadRequestObjectResult>(guardedResult);

        // Deletion with force=true should succeed
        var forceResult = await controller.DeleteCourse(course.Id, force: true);
        Assert.IsType<NoContentResult>(forceResult);

        // Verify course and offering are removed
        Assert.Null(await context.Courses.FindAsync(course.Id));
        Assert.Null(await context.CourseOfferings.FindAsync(offering.Id));
    }

    // =========================================================================
    // AdminOfferingsController Tests
    // =========================================================================

    [Fact]
    public async Task Offerings_CreateGetHierarchyAndDelete_ExecutesCleanly()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, lvl1, _, term) = await SeedBaseCatalogAsync(context);

        var course = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = lvl1.Id, Code = "CS101", Name = "Intro" };
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();

        var offeringsController = new AdminOfferingsController(context);
        var slotsController = new AdminScheduleSlotsController(context);

        // 1. Create Offering
        var createResult = await offeringsController.CreateOffering(new CreateOfferingRequest
        {
            CourseId = course.Id,
            TermId = term.Id
        });
        var okDetail = Assert.IsType<OkObjectResult>(createResult.Result);
        var offeringDto = Assert.IsType<AdminOfferingDetailDto>(okDetail.Value);
        Assert.Equal("CS101", offeringDto.CourseCode);

        // Duplicate offering in same term rejected
        var dupResult = await offeringsController.CreateOffering(new CreateOfferingRequest
        {
            CourseId = course.Id,
            TermId = term.Id
        });
        Assert.IsType<BadRequestObjectResult>(dupResult.Result);

        // 2. Add Activity (Lecture)
        var actResult = await slotsController.CreateActivity(offeringDto.Id, new CreateActivityRequest
        {
            Type = ActivityType.Lecture
        });
        var createdAct = Assert.IsType<ObjectResult>(actResult);
        Assert.Equal(201, createdAct.StatusCode);
        var actData = Assert.IsType<AdminActivityResponse>(createdAct.Value);
        Guid actId = actData.Id;

        // 3. Add Activity Group (Group 1)
        var grpResult = await slotsController.CreateActivityGroup(actId, new CreateActivityGroupRequest
        {
            Name = "Group 1"
        });
        var createdGrp = Assert.IsType<ObjectResult>(grpResult);
        Assert.Equal(201, createdGrp.StatusCode);
        var grpData = Assert.IsType<AdminActivityGroupResponse>(createdGrp.Value);
        Guid grpId = grpData.Id;

        // 4. Add Meeting slot (Saturday 09:00 - 10:30)
        var meetResult = await slotsController.CreateMeeting(grpId, new CreateMeetingRequest
        {
            DayOfWeek = DayOfWeek.Saturday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Room = "Auditorium A"
        });
        var createdMeet = Assert.IsType<ObjectResult>(meetResult);
        Assert.Equal(201, createdMeet.StatusCode);

        // 5. Retrieve hierarchical detail
        var detailResult = await offeringsController.GetOfferingById(offeringDto.Id);
        var okHier = Assert.IsType<OkObjectResult>(detailResult.Result);
        var hierDto = Assert.IsType<AdminOfferingDetailDto>(okHier.Value);
        Assert.Single(hierDto.Activities);
        Assert.Single(hierDto.Activities[0].Groups);
        Assert.Single(hierDto.Activities[0].Groups[0].Meetings);
        Assert.Equal("Auditorium A", hierDto.Activities[0].Groups[0].Meetings[0].Room);

        // 6. Delete offering (cascades to all children)
        var deleteResult = await offeringsController.DeleteOffering(offeringDto.Id);
        Assert.IsType<NoContentResult>(deleteResult);
        Assert.Empty(await context.CourseOfferings.Where(o => o.Id == offeringDto.Id).ToListAsync());
        Assert.Empty(await context.Activities.Where(a => a.Id == actId).ToListAsync());
        Assert.Empty(await context.ActivityGroups.Where(g => g.Id == grpId).ToListAsync());
        Assert.Empty(await context.Meetings.ToListAsync());
    }

    // =========================================================================
    // AdminScheduleSlotsController Meeting Validation Tests
    // =========================================================================

    [Fact]
    public async Task Meetings_Validation_RejectsInvalidTimeAndInternalOverlap()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);
        var (uni, lvl1, _, term) = await SeedBaseCatalogAsync(context);

        var course = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = lvl1.Id, Code = "CS101", Name = "Intro" };
        var offering = new CourseOffering { Id = Guid.NewGuid(), CourseId = course.Id, TermId = term.Id, UniversityId = uni.Id };
        var act = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offering.Id, Type = ActivityType.Lecture };
        var grp = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = act.Id, Name = "G1" };

        await context.Courses.AddAsync(course);
        await context.CourseOfferings.AddAsync(offering);
        await context.Activities.AddAsync(act);
        await context.ActivityGroups.AddAsync(grp);
        await context.SaveChangesAsync();

        var slotsController = new AdminScheduleSlotsController(context);

        // 1. Invalid Time: StartTime >= EndTime
        var invalidTime = await slotsController.CreateMeeting(grp.Id, new CreateMeetingRequest
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(10, 0),
            Room = "Lab 1"
        });
        Assert.IsType<BadRequestObjectResult>(invalidTime);

        // 2. Add valid meeting 10:00 - 12:00
        var validMeet = await slotsController.CreateMeeting(grp.Id, new CreateMeetingRequest
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            Room = "Lab 1"
        });
        var created = Assert.IsType<ObjectResult>(validMeet);
        Assert.Equal(201, created.StatusCode);

        // 3. Overlapping meeting in same group (11:00 - 13:00 on Sunday)
        var overlapping = await slotsController.CreateMeeting(grp.Id, new CreateMeetingRequest
        {
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(13, 0),
            Room = "Lab 2"
        });
        Assert.IsType<BadRequestObjectResult>(overlapping);
    }

    // =========================================================================
    // AdminTermsController Tests
    // =========================================================================

    [Fact]
    public async Task Terms_CreateAndSetCurrent_TogglesAtomically()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateInMemoryContext(dbName);

        var controller = new AdminTermsController(context);

        // 1. Create Fall 2026 (Current)
        var term1Result = await controller.CreateTerm(new CreateTermRequest
        {
            Semester = Semester.Fall,
            Year = 2026,
            IsCurrent = true
        });
        var ok1 = Assert.IsType<ObjectResult>(term1Result.Result);
        var t1 = Assert.IsType<AdminTermDto>(ok1.Value);
        Assert.True(t1.IsCurrent);

        // 2. Create Spring 2027 (Not current initially)
        var term2Result = await controller.CreateTerm(new CreateTermRequest
        {
            Semester = Semester.Spring,
            Year = 2027,
            IsCurrent = false
        });
        var ok2 = Assert.IsType<ObjectResult>(term2Result.Result);
        var t2 = Assert.IsType<AdminTermDto>(ok2.Value);
        Assert.False(t2.IsCurrent);

        // 3. Duplicate term (Fall 2026) rejected
        var dupResult = await controller.CreateTerm(new CreateTermRequest
        {
            Semester = Semester.Fall,
            Year = 2026,
            IsCurrent = false
        });
        Assert.IsType<BadRequestObjectResult>(dupResult.Result);

        // 4. Set Spring 2027 as current
        var setCurrentResult = await controller.SetCurrentTerm(t2.Id);
        var okSet = Assert.IsType<OkObjectResult>(setCurrentResult.Result);
        var updatedT2 = Assert.IsType<AdminTermDto>(okSet.Value);
        Assert.True(updatedT2.IsCurrent);

        // Verify in DB that Fall 2026 became false
        var refreshedT1 = await context.Terms.FindAsync(t1.Id);
        Assert.False(refreshedT1!.IsCurrent);
    }
}
