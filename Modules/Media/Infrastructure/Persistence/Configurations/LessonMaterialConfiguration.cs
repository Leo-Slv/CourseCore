using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Configurations;

public class LessonMaterialConfiguration : IEntityTypeConfiguration<LessonMaterialPersistenceModel>
{
    public void Configure(EntityTypeBuilder<LessonMaterialPersistenceModel> builder)
    {
        builder.ToTable("lesson_materials");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LessonId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(x => x.StorageProvider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.LessonId);
        builder.HasIndex(x => new { x.LessonId, x.DisplayOrder });

        builder
            .HasOne(x => x.Lesson)
            .WithMany(x => x.Materials)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
