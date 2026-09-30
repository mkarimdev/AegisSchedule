using AegisSchedule.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisSchedule.Api.Persistence.Configurations;

public class ActivityGroupConfiguration : IEntityTypeConfiguration<ActivityGroup>
{
    public void Configure(EntityTypeBuilder<ActivityGroup> builder)
    {
        builder.ToTable("ActivityGroups");

        builder.HasKey(ag => ag.Id);

        builder.Property(ag => ag.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne<Activity>()
            .WithMany()
            .HasForeignKey(ag => ag.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
