using AegisSchedule.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisSchedule.Api.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne<University>()
            .WithMany()
            .HasForeignKey(c => c.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicLevel>()
            .WithMany()
            .HasForeignKey(c => c.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
