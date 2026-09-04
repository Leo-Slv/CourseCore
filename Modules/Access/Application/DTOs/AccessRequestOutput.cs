using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class AccessRequestOutput
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? DecidedAt { get; init; }

    public Guid? DecidedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }

    public static AccessRequestOutput FromAccessRequest(AccessRequest request)
    {
        return new AccessRequestOutput
        {
            Id = request.Id,
            UserId = request.UserId,
            CourseId = request.CourseId,
            Status = request.Status.ToString(),
            DecidedAt = request.DecidedAt,
            DecidedByUserId = request.DecidedByUserId,
            CreatedAt = request.CreatedAt
        };
    }
}
