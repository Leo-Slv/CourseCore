using CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogPersistenceModel>
{
    public void Configure(EntityTypeBuilder<AuditLogPersistenceModel> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired(false);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(150);
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.EntityId).IsRequired(false);
        builder.Property(x => x.MetadataJson).IsRequired(false).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.CreatedAt);

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
