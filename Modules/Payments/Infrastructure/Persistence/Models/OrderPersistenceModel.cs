using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Models;

public class OrderPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CourseId { get; set; }

    public long AmountInCents { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int Status { get; set; }

    public string? StripeCheckoutSessionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }

    public CoursePersistenceModel? Course { get; set; }
}
