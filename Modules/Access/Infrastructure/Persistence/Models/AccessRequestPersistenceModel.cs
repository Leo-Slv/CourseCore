namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class AccessRequestPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CourseId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? DecidedAt { get; set; }

    public Guid? DecidedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
