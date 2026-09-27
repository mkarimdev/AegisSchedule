using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(UniSchedulingDbContext context)
    {
        // 1. Ensure the PostgreSQL database schema is created and all migrations applied
        await context.Database.MigrateAsync();

        // 2. Prevent duplicate seeding
        if (await context.Universities.AnyAsync())
        {
            return;
        }

        // 3. Seed University
        var university = new University
        {
            Id = Guid.NewGuid(),
            Name = "Minia National University"
        };
        await context.Universities.AddAsync(university);

        // 4. Seed Academic Levels
        var level1 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 1 };
        var level2 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 2 };
        var level3 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 3 };
        var level4 = new AcademicLevel { Id = Guid.NewGuid(), LevelNumber = 4 };
        await context.AcademicLevels.AddRangeAsync(level1, level2, level3, level4);

        // 5. Seed Term (Fall 2026)
        var term = new Term
        {
            Id = Guid.NewGuid(),
            Semester = Semester.Fall,
            Year = 2026,
            IsCurrent = true
        };
        await context.Terms.AddAsync(term);

        // 6. Seed Courses
        var cs101 = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = university.Id,
            AcademicLevelId = level1.Id,
            Code = "CS101",
            Name = "Intro to Programming"
        };
        var math101 = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = university.Id,
            AcademicLevelId = level1.Id,
            Code = "MATH101",
            Name = "Calculus & Linear Algebra"
        };
        var cs201 = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = university.Id,
            AcademicLevelId = level2.Id,
            Code = "CS201",
            Name = "Database Systems"
        };
        var cs202 = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = university.Id,
            AcademicLevelId = level2.Id,
            Code = "CS202",
            Name = "Data Structures"
        };
        var cs301 = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = university.Id,
            AcademicLevelId = level3.Id,
            Code = "CS301",
            Name = "Artificial Intelligence"
        };
        var cs302 = new Course
        {
            Id = Guid.NewGuid(),
            UniversityId = university.Id,
            AcademicLevelId = level3.Id,
            Code = "CS302",
            Name = "Software Engineering"
        };

        await context.Courses.AddRangeAsync(cs101, math101, cs201, cs202, cs301, cs302);

        // 7. Seed Course Offerings
        var offeringCs101 = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = cs101.Id,
            TermId = term.Id,
            UniversityId = university.Id
        };
        var offeringMath101 = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = math101.Id,
            TermId = term.Id,
            UniversityId = university.Id
        };
        var offeringCs201 = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = cs201.Id,
            TermId = term.Id,
            UniversityId = university.Id
        };
        var offeringCs202 = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = cs202.Id,
            TermId = term.Id,
            UniversityId = university.Id
        };
        var offeringCs301 = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = cs301.Id,
            TermId = term.Id,
            UniversityId = university.Id
        };
        var offeringCs302 = new CourseOffering
        {
            Id = Guid.NewGuid(),
            CourseId = cs302.Id,
            TermId = term.Id,
            UniversityId = university.Id
        };

        await context.CourseOfferings.AddRangeAsync(
            offeringCs101,
            offeringMath101,
            offeringCs201,
            offeringCs202,
            offeringCs301,
            offeringCs302
        );

        // 8. Seed Activities, Groups, and Meetings

        // --- CS101: Lecture (G1, G2) & Lab (Sec1, Sec2) ---
        var cs101Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs101.Id, Type = ActivityType.Lecture };
        var cs101Lab = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs101.Id, Type = ActivityType.Lab };
        await context.Activities.AddRangeAsync(cs101Lec, cs101Lab);

        var cs101LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lec.Id, Name = "Group 1" };
        var cs101LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lec.Id, Name = "Group 2" };
        var cs101LabS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lab.Id, Name = "Section 1" };
        var cs101LabS2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs101Lab.Id, Name = "Section 2" };
        await context.ActivityGroups.AddRangeAsync(cs101LecG1, cs101LecG2, cs101LabS1, cs101LabS2);

        await context.Meetings.AddRangeAsync(
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs101LecG1.Id, DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Hall 101" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs101LecG2.Id, DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Hall 101" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs101LabS1.Id, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Lab A" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs101LabS2.Id, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Lab A" }
        );

        // --- MATH101: Lecture (G1, G2) & Tutorial (Sec1, Sec2) ---
        var math101Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringMath101.Id, Type = ActivityType.Lecture };
        var math101Tut = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringMath101.Id, Type = ActivityType.Tutorial };
        await context.Activities.AddRangeAsync(math101Lec, math101Tut);

        var math101LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = math101Lec.Id, Name = "Group 1" };
        var math101LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = math101Lec.Id, Name = "Group 2" };
        var math101TutS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = math101Tut.Id, Name = "Section 1" };
        var math101TutS2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = math101Tut.Id, Name = "Section 2" };
        await context.ActivityGroups.AddRangeAsync(math101LecG1, math101LecG2, math101TutS1, math101TutS2);

        await context.Meetings.AddRangeAsync(
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = math101LecG1.Id, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Hall 102" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = math101LecG2.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Hall 102" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = math101TutS1.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Tutorial Room 1" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = math101TutS2.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), Room = "Tutorial Room 1" }
        );

        // --- CS201: Lecture (G1, G2) & Lab (Sec1, Sec2) ---
        var cs201Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs201.Id, Type = ActivityType.Lecture };
        var cs201Lab = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs201.Id, Type = ActivityType.Lab };
        await context.Activities.AddRangeAsync(cs201Lec, cs201Lab);

        var cs201LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs201Lec.Id, Name = "Group 1" };
        var cs201LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs201Lec.Id, Name = "Group 2" };
        var cs201LabS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs201Lab.Id, Name = "Section 1" };
        var cs201LabS2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs201Lab.Id, Name = "Section 2" };
        await context.ActivityGroups.AddRangeAsync(cs201LecG1, cs201LecG2, cs201LabS1, cs201LabS2);

        await context.Meetings.AddRangeAsync(
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs201LecG1.Id, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Hall 201" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs201LecG2.Id, DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), Room = "Hall 201" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs201LabS1.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Database Lab" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs201LabS2.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Database Lab" }
        );

        // --- CS202: Lecture (G1, G2) & Lab (Sec1, Sec2) ---
        var cs202Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs202.Id, Type = ActivityType.Lecture };
        var cs202Lab = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs202.Id, Type = ActivityType.Lab };
        await context.Activities.AddRangeAsync(cs202Lec, cs202Lab);

        var cs202LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs202Lec.Id, Name = "Group 1" };
        var cs202LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs202Lec.Id, Name = "Group 2" };
        var cs202LabS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs202Lab.Id, Name = "Section 1" };
        var cs202LabS2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs202Lab.Id, Name = "Section 2" };
        await context.ActivityGroups.AddRangeAsync(cs202LecG1, cs202LecG2, cs202LabS1, cs202LabS2);

        await context.Meetings.AddRangeAsync(
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs202LecG1.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Hall 202" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs202LecG2.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), Room = "Hall 202" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs202LabS1.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Algorithms Lab" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs202LabS2.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Algorithms Lab" }
        );

        // --- CS301: Lecture (G1, G2) & Lab (Sec1, Sec2) ---
        var cs301Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs301.Id, Type = ActivityType.Lecture };
        var cs301Lab = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs301.Id, Type = ActivityType.Lab };
        await context.Activities.AddRangeAsync(cs301Lec, cs301Lab);

        var cs301LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs301Lec.Id, Name = "Group 1" };
        var cs301LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs301Lec.Id, Name = "Group 2" };
        var cs301LabS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs301Lab.Id, Name = "Section 1" };
        var cs301LabS2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs301Lab.Id, Name = "Section 2" };
        await context.ActivityGroups.AddRangeAsync(cs301LecG1, cs301LecG2, cs301LabS1, cs301LabS2);

        await context.Meetings.AddRangeAsync(
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs301LecG1.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Hall 301" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs301LecG2.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Hall 301" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs301LabS1.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "AI Lab" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs301LabS2.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), Room = "AI Lab" }
        );

        // --- CS302: Lecture (G1, G2) & Lab (Sec1, Sec2) ---
        var cs302Lec = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs302.Id, Type = ActivityType.Lecture };
        var cs302Lab = new Activity { Id = Guid.NewGuid(), CourseOfferingId = offeringCs302.Id, Type = ActivityType.Lab };
        await context.Activities.AddRangeAsync(cs302Lec, cs302Lab);

        var cs302LecG1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs302Lec.Id, Name = "Group 1" };
        var cs302LecG2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs302Lec.Id, Name = "Group 2" };
        var cs302LabS1 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs302Lab.Id, Name = "Section 1" };
        var cs302LabS2 = new ActivityGroup { Id = Guid.NewGuid(), ActivityId = cs302Lab.Id, Name = "Section 2" };
        await context.ActivityGroups.AddRangeAsync(cs302LecG1, cs302LecG2, cs302LabS1, cs302LabS2);

        await context.Meetings.AddRangeAsync(
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs302LecG1.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), Room = "Hall 302" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs302LecG2.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 30), Room = "Hall 302" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs302LabS1.Id, DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(12, 30), Room = "Software Lab" },
            new Meeting { Id = Guid.NewGuid(), ActivityGroupId = cs302LabS2.Id, DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), Room = "Software Lab" }
        );

        // 9. Save all changes
        await context.SaveChangesAsync();
    }
}
