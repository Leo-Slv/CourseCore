using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Configurations;

public class UserCourseProgressConfiguration : IEntityTypeConfiguration<UserCourseProgressPersistenceModel>
{
    public void Configure(EntityTypeBuilder<UserCourseProgressPersistenceModel> builder)
    {
        builder.ToTable("user_course_progress");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.ProgressPercent).IsRequired().HasPrecision(5, 2);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();

        builder
            .HasOne(x => x.User)
            .WithMany(x => x.CourseProgresses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Course)
            .WithMany(x => x.UserProgresses)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
