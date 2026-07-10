using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Configurations;

public class VideoConfiguration : IEntityTypeConfiguration<VideoPersistenceModel>
{
    public void Configure(EntityTypeBuilder<VideoPersistenceModel> builder)
    {
        builder.ToTable("videos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LessonId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.StorageProvider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.PlaybackUrl).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.ThumbnailUrl).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.DurationSeconds).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.LessonId).IsUnique();
        builder.HasIndex(x => x.Status);

        builder
            .HasOne(x => x.Lesson)
            .WithOne(x => x.Video)
            .HasForeignKey<VideoPersistenceModel>(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
