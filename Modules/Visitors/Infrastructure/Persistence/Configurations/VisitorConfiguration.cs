using CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Configurations;

public class VisitorConfiguration : IEntityTypeConfiguration<VisitorPersistenceModel>
{
    public void Configure(EntityTypeBuilder<VisitorPersistenceModel> builder)
    {
        builder.ToTable("visitors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(11);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.Address).IsRequired(false).HasMaxLength(300);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Property(x => x.ContactedAt).IsRequired(false);
        builder.Property(x => x.ContactedByUserId).IsRequired(false);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ContactedAt);
    }
}
