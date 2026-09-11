using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Configurations;

public class LessonNoteConfiguration : IEntityTypeConfiguration<LessonNotePersistenceModel>
{
    public void Configure(EntityTypeBuilder<LessonNotePersistenceModel> builder)
    {
        builder.ToTable("lesson_notes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.LessonId).IsRequired();
        builder.Property(x => x.Content).IsRequired().HasMaxLength(10_000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LessonId);
        builder.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();

        builder
            .HasOne(x => x.User)
            .WithMany(x => x.LessonNotes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Lesson)
            .WithMany(x => x.UserNotes)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
