using Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Persistence.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne<CourseOffering>()
            .WithMany()
            .HasForeignKey(a => a.CourseOfferingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
