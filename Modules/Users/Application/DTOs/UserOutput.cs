using CourseCore.Api.Modules.Users.Domain.Entities;

namespace CourseCore.Api.Modules.Users.Application.DTOs;

public class UserOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool Active { get; init; }

    public DateTime? EmailVerifiedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static UserOutput FromUser(User user)
    {
        return new UserOutput
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Active = user.Active,
            EmailVerifiedAt = user.EmailVerifiedAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
