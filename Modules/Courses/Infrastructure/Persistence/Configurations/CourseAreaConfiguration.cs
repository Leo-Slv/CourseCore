using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Configurations;

public class CourseAreaConfiguration : IEntityTypeConfiguration<CourseAreaPersistenceModel>
{
    public void Configure(EntityTypeBuilder<CourseAreaPersistenceModel> builder)
    {
        builder.ToTable("course_areas");

        builder.HasKey(x => new { x.CourseId, x.AreaId });

        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.AreaId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder
            .HasOne(x => x.Course)
            .WithMany(x => x.CourseAreas)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Area)
            .WithMany(x => x.CourseAreas)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
