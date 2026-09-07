using CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Configurations;

public class TestimonialConfiguration : IEntityTypeConfiguration<TestimonialPersistenceModel>
{
    public void Configure(EntityTypeBuilder<TestimonialPersistenceModel> builder)
    {
        builder.ToTable("testimonials");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuthorName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Quote).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.AvatarUrl).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.Published).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Published);

        builder
            .HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
