using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Configurations;

public class RoleAreaAccessConfiguration : IEntityTypeConfiguration<RoleAreaAccessPersistenceModel>
{
    public void Configure(EntityTypeBuilder<RoleAreaAccessPersistenceModel> builder)
    {
        builder.ToTable("role_area_accesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleId).IsRequired();
        builder.Property(x => x.AreaId).IsRequired();
        builder.Property(x => x.CanView).IsRequired();
        builder.Property(x => x.CanManage).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => x.AreaId);
        builder.HasIndex(x => new { x.RoleId, x.AreaId }).IsUnique();

        builder
            .HasOne(x => x.Role)
            .WithMany(x => x.AreaAccesses)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Area)
            .WithMany(x => x.RoleAccesses)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
