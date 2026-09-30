using AegisSchedule.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisSchedule.Api.Persistence.Configurations;

public class CourseOfferingConfiguration : IEntityTypeConfiguration<CourseOffering>
{
    public void Configure(EntityTypeBuilder<CourseOffering> builder)
    {
        builder.ToTable("CourseOfferings");

        builder.HasKey(co => co.Id);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(co => co.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Term>()
            .WithMany()
            .HasForeignKey(co => co.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<University>()
            .WithMany()
            .HasForeignKey(co => co.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
