using AegisSchedule.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisSchedule.Api.Persistence.Configurations;

public class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> builder)
    {
        builder.ToTable("Terms");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Semester)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Year)
            .IsRequired();

        builder.Property(t => t.IsCurrent)
            .IsRequired();
    }
}
