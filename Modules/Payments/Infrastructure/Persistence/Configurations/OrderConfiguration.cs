using CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderPersistenceModel>
{
    public void Configure(EntityTypeBuilder<OrderPersistenceModel> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.AmountInCents).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.StripeCheckoutSessionId).IsRequired(false).HasMaxLength(255);
        builder.Property(x => x.PaidAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.StripeCheckoutSessionId);

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
