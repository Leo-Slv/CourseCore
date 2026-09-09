using CourseCore.Api.Modules.Users.Domain.Entities;

namespace CourseCore.Api.Modules.Users.Application.DTOs;

public class UserOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }

    public DateTime? EmailVerifiedAt { get; init; }

    public IReadOnlyCollection<string> RoleNames { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> AreaNames { get; init; } = Array.Empty<string>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static UserOutput FromUser(
        User user,
        IReadOnlyCollection<string>? roleNames = null,
        IReadOnlyCollection<string>? areaNames = null)
    {
        return new UserOutput
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Active = user.Active,
            EmailVerifiedAt = user.EmailVerifiedAt,
            RoleNames = roleNames ?? Array.Empty<string>(),
            AreaNames = areaNames ?? Array.Empty<string>(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
