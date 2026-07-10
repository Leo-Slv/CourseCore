using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Configurations;

public class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgressPersistenceModel>
{
    public void Configure(EntityTypeBuilder<UserLessonProgressPersistenceModel> builder)
    {
        builder.ToTable("user_lesson_progress");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.LessonId).IsRequired();
        builder.Property(x => x.Completed).IsRequired();
        builder.Property(x => x.WatchedSeconds).IsRequired();
        builder.Property(x => x.LastWatchedAt).IsRequired();
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LessonId);
        builder.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();

        builder
            .HasOne(x => x.User)
            .WithMany(x => x.LessonProgresses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Lesson)
            .WithMany(x => x.UserProgresses)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
