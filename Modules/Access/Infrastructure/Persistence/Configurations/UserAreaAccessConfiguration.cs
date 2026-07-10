using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Configurations;

public class UserAreaAccessConfiguration : IEntityTypeConfiguration<UserAreaAccessPersistenceModel>
{
    public void Configure(EntityTypeBuilder<UserAreaAccessPersistenceModel> builder)
    {
        builder.ToTable("user_area_accesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.AreaId).IsRequired();
        builder.Property(x => x.CanView).IsRequired();
        builder.Property(x => x.CanManage).IsRequired();
        builder.Property(x => x.StartsAt).IsRequired(false);
        builder.Property(x => x.ExpiresAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AreaId);
        builder.HasIndex(x => new { x.UserId, x.AreaId }).IsUnique();

        builder
            .HasOne(x => x.User)
            .WithMany(x => x.AreaAccesses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Area)
            .WithMany(x => x.UserAccesses)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
