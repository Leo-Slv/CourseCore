using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<AreaPersistenceModel>
{
    public void Configure(EntityTypeBuilder<AreaPersistenceModel> builder)
    {
        builder.ToTable("areas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(180);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Active).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Slug).IsUnique();

        builder
            .HasMany(x => x.CourseAreas)
            .WithOne(x => x.Area)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.UserAccesses)
            .WithOne(x => x.Area)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.RoleAccesses)
            .WithOne(x => x.Area)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
