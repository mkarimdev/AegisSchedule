using AegisSchedule.Api.Controllers;
using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;
using AegisSchedule.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AegisSchedule.Tests;

public class CatalogControllerTests
{
    private static UniSchedulingDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<UniSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new UniSchedulingDbContext(options);
    }

    private static async Task SeedStandardCatalogAsync(UniSchedulingDbContext context)
    {
        var uni = new University { Id = Guid.NewGuid(), Name = "Minia National University" };
        var level1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var level2 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 2 };
        var level3 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 3 };

        var term = new Term { Id = Guid.NewGuid(), Semester = Semester.Fall, Year = 2026, IsCurrent = true };

        var cs101 = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "CS101", Name = "Intro to Programming" };
        var math101 = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level1.Id, Code = "MATH101", Name = "Calculus" };
        var cs201 = new Course { Id = Guid.NewGuid(), UniversityId = uni.Id, AcademicLevelId = level2.Id, Code = "CS201", Name = "Database Systems" };

        var offeringCs101 = new CourseOffering { Id = Guid.NewGuid(), CourseId = cs101.Id, TermId = term.Id, UniversityId = uni.Id };
        var offeringMath101 = new CourseOffering { Id = Guid.NewGuid(), CourseId = math101.Id, TermId = term.Id, UniversityId = uni.Id };
        var offeringCs201 = new CourseOffering { Id = Guid.NewGuid(), CourseId = cs201.Id, TermId = term.Id, UniversityId = uni.Id };

        var cs101Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs101.Id, Type = ActivityType.Lecture };
        var cs101Lab = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs101.Id, Type = ActivityType.Lab };
        var math101Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringMath101.Id, Type = ActivityType.Lecture };

        var cs101LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lec.Id, Name = "Group 1" };
        var cs101LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lec.Id, Name = "Group 2" };
        var cs101LabS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lab.Id, Name = "Section 1" };

        await context.Universities.AddAsync(uni);
        await context.AcademicLevels.AddRangeAsync(level3, level1, level2); // intentionally out of order
        await context.Terms.AddAsync(term);
        await context.Courses.AddRangeAsync(cs101, math101, cs201);
        await context.CourseOfferings.AddRangeAsync(offeringCs101, offeringMath101, offeringCs201);
        await context.Activities.AddRangeAsync(cs101Lec, cs101Lab, math101Lec);
        await context.ActivityGroups.AddRangeAsync(cs101LecG1, cs101LecG2, cs101LabS1);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLevels_ReturnsAscendingOrderedAcademicLevelsWithDisplayNames()
    {
        // Arrange
        using var context = CreateInMemoryContext(nameof(GetLevels_ReturnsAscendingOrderedAcademicLevelsWithDisplayNames));
        await SeedStandardCatalogAsync(context);
        var controller = new CatalogController(context);

        // Act
        var result = await controller.GetLevels();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var levels = Assert.IsType<List<AcademicLevelDto>>(okResult.Value);

        Assert.Equal(3, levels.Count);
        Assert.Equal(1, levels[0].LevelNumber);
        Assert.Equal("Level 1", levels[0].DisplayName);
        Assert.Equal(2, levels[1].LevelNumber);
        Assert.Equal("Level 2", levels[1].DisplayName);
        Assert.Equal(3, levels[2].LevelNumber);
        Assert.Equal("Level 3", levels[2].DisplayName);
    }

    [Fact]
    public async Task GetOfferings_WithoutFilters_ReturnsAllOfferingsAcrossAllLevels()
    {
        // Arrange
        using var context = CreateInMemoryContext(nameof(GetOfferings_WithoutFilters_ReturnsAllOfferingsAcrossAllLevels));
        await SeedStandardCatalogAsync(context);
        var controller = new CatalogController(context);

        // Act
        var result = await controller.GetOfferings(academicLevel: null, termId: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var offerings = Assert.IsType<List<CourseOfferingDto>>(okResult.Value);

        Assert.Equal(3, offerings.Count);
        Assert.Contains(offerings, o => o.CourseCode == "CS101");
        Assert.Contains(offerings, o => o.CourseCode == "MATH101");
        Assert.Contains(offerings, o => o.CourseCode == "CS201");
    }

    [Fact]
    public async Task GetOfferings_WithAcademicLevelFilter_ReturnsOnlyMatchingOfferings()
    {
        // Arrange
        using var context = CreateInMemoryContext(nameof(GetOfferings_WithAcademicLevelFilter_ReturnsOnlyMatchingOfferings));
        await SeedStandardCatalogAsync(context);
        var controller = new CatalogController(context);

        // Act: Filter by Academic Level 2
        var resultLevel2 = await controller.GetOfferings(academicLevel: 2, termId: null);

        // Assert
        var okResult2 = Assert.IsType<OkObjectResult>(resultLevel2.Result);
        var offeringsLevel2 = Assert.IsType<List<CourseOfferingDto>>(okResult2.Value);

        Assert.Single(offeringsLevel2);
        Assert.Equal("CS201", offeringsLevel2[0].CourseCode);
        Assert.Equal(2, offeringsLevel2[0].AcademicLevelNumber);

        // Act: Filter by Academic Level 1
        var resultLevel1 = await controller.GetOfferings(academicLevel: 1, termId: null);

        // Assert
        var okResult1 = Assert.IsType<OkObjectResult>(resultLevel1.Result);
        var offeringsLevel1 = Assert.IsType<List<CourseOfferingDto>>(okResult1.Value);

        Assert.Equal(2, offeringsLevel1.Count);
        Assert.All(offeringsLevel1, o => Assert.Equal(1, o.AcademicLevelNumber));
    }

    [Fact]
    public async Task GetOfferings_MapsCourseDetailsActivitiesAndGroupsAccurately()
    {
        // Arrange
        using var context = CreateInMemoryContext(nameof(GetOfferings_MapsCourseDetailsActivitiesAndGroupsAccurately));
        await SeedStandardCatalogAsync(context);
        var controller = new CatalogController(context);

        // Act
        var result = await controller.GetOfferings(academicLevel: 1, termId: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var offerings = Assert.IsType<List<CourseOfferingDto>>(okResult.Value);

        var cs101 = offerings.FirstOrDefault(o => o.CourseCode == "CS101");
        Assert.NotNull(cs101);
        Assert.Equal("Intro to Programming", cs101.CourseName);
        Assert.Equal("Fall 2026", cs101.TermName);
        Assert.Equal(2, cs101.ActivityTypes.Count);
        Assert.Contains("Lecture", cs101.ActivityTypes);
        Assert.Contains("Lab", cs101.ActivityTypes);

        // Groups: 2 lecture groups + 1 lab section
        Assert.Equal(3, cs101.Groups.Count);
        Assert.Contains(cs101.Groups, g => g.Name == "Group 1" && g.ActivityType == "Lecture");
        Assert.Contains(cs101.Groups, g => g.Name == "Group 2" && g.ActivityType == "Lecture");
        Assert.Contains(cs101.Groups, g => g.Name == "Section 1" && g.ActivityType == "Lab");
    }

    [Fact]
    public async Task GetOfferings_WhenLevelHasNoOfferings_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext(nameof(GetOfferings_WhenLevelHasNoOfferings_ReturnsEmptyList));
        await SeedStandardCatalogAsync(context);
        var controller = new CatalogController(context);

        // Act: Filter by Academic Level 4 (no offerings seeded)
        var result = await controller.GetOfferings(academicLevel: 4, termId: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var offerings = Assert.IsType<List<CourseOfferingDto>>(okResult.Value);

        Assert.Empty(offerings);
    }
}
