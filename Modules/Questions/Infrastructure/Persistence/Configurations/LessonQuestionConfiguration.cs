using CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Configurations;

public class LessonQuestionConfiguration : IEntityTypeConfiguration<LessonQuestionPersistenceModel>
{
    public void Configure(EntityTypeBuilder<LessonQuestionPersistenceModel> builder)
    {
        builder.ToTable("lesson_questions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LessonId).IsRequired();
        builder.Property(x => x.AskedByUserId).IsRequired();
        builder.Property(x => x.AskedByName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.QuestionText).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.AnswerText).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.AnsweredByUserId).IsRequired(false);
        builder.Property(x => x.AnsweredByName).IsRequired(false).HasMaxLength(150);
        builder.Property(x => x.AnsweredAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.LessonId);

        builder
            .HasOne(x => x.Lesson)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
