using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

public class UniSchedulingDbContext : DbContext
{
    public UniSchedulingDbContext(DbContextOptions<UniSchedulingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Term> Terms => Set<Term>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<AcademicLevel> AcademicLevels => Set<AcademicLevel>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityGroup> ActivityGroups => Set<ActivityGroup>();
    public DbSet<Meeting> Meetings => Set<Meeting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UniSchedulingDbContext).Assembly);
    }
}
