using AegisSchedule.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisSchedule.Api.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.StartTime)
            .IsRequired();

        builder.Property(m => m.EndTime)
            .IsRequired();

        builder.Property(m => m.Room)
            .HasMaxLength(100);

        builder.HasOne<ActivityGroup>()
            .WithMany()
            .HasForeignKey(m => m.ActivityGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
