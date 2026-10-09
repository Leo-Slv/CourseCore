using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Configurations;

public class UserCourseAccessConfiguration : IEntityTypeConfiguration<UserCourseAccessPersistenceModel>
{
    public void Configure(EntityTypeBuilder<UserCourseAccessPersistenceModel> builder)
    {
        builder.ToTable("user_course_accesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
