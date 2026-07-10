using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<LessonPersistenceModel>
{
    public void Configure(EntityTypeBuilder<LessonPersistenceModel> builder)
    {
        builder.ToTable("lessons");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModuleId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.FreePreview).IsRequired();
        builder.Property(x => x.Published).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.ModuleId);
        builder.HasIndex(x => new { x.ModuleId, x.DisplayOrder }).IsUnique();

        builder
            .HasOne(x => x.Module)
            .WithMany(x => x.Lessons)
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.UserProgresses)
            .WithOne(x => x.Lesson)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
