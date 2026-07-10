namespace CourseCore.Api.Shared.Domain.Entities;

public abstract class EntityBase
{
    protected EntityBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }

    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
