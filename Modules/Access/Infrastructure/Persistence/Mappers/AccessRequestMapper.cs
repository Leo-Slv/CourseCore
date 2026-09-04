using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class AccessRequestMapper
{
    public static AccessRequest ToDomain(AccessRequestPersistenceModel model)
    {
        return AccessRequest.Restore(
            model.Id,
            model.UserId,
            model.CourseId,
            ParseStatus(model.Status),
            model.DecidedAt,
            model.DecidedByUserId,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static AccessRequestPersistenceModel ToPersistence(AccessRequest request)
    {
        return new AccessRequestPersistenceModel
        {
            Id = request.Id,
            UserId = request.UserId,
            CourseId = request.CourseId,
            Status = request.Status.ToString(),
            DecidedAt = request.DecidedAt,
            DecidedByUserId = request.DecidedByUserId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    public static void ApplyChanges(AccessRequest request, AccessRequestPersistenceModel model)
    {
        model.Status = request.Status.ToString();
        model.DecidedAt = request.DecidedAt;
        model.DecidedByUserId = request.DecidedByUserId;
        model.UpdatedAt = request.UpdatedAt;
    }

    private static AccessRequestStatus ParseStatus(string value)
    {
        if (Enum.TryParse<AccessRequestStatus>(value, ignoreCase: true, out var status))
        {
            return status;
        }

        throw new InvalidOperationException($"Unknown access request status '{value}'.");
    }
}
