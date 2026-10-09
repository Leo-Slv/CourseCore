using CourseCore.Api.Modules.Visitors.Domain.Entities;

namespace CourseCore.Api.Modules.Visitors.Application.DTOs;

public sealed class VisitorOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Address { get; init; }

    public DateTime SubmittedAt { get; init; }

    public bool Contacted { get; init; }

    public DateTime? ContactedAt { get; init; }

    public static VisitorOutput FromVisitor(Visitor visitor)
    {
        return new VisitorOutput
        {
            Id = visitor.Id,
            Name = visitor.Name,
            Phone = visitor.Phone,
            Email = visitor.Email.Value,
            Address = visitor.Address,
            SubmittedAt = visitor.CreatedAt,
            Contacted = visitor.IsContacted,
            ContactedAt = visitor.ContactedAt
        };
    }
}
