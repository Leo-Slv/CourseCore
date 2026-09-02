namespace CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Models;

public class EmailVerificationTokenPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }
}
