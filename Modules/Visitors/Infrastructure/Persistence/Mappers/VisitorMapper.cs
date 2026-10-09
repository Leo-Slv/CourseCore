using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Mappers;

public static class VisitorMapper
{
    public static Visitor ToDomain(VisitorPersistenceModel model)
    {
        return Visitor.Restore(
            model.Id,
            model.Name,
            model.Phone,
            Email.Create(model.Email),
            model.Address,
            model.CreatedAt,
            model.UpdatedAt,
            model.ContactedAt,
            model.ContactedByUserId);
    }

    public static VisitorPersistenceModel ToPersistence(Visitor visitor)
    {
        return new VisitorPersistenceModel
        {
            Id = visitor.Id,
            Name = visitor.Name,
            Phone = visitor.Phone,
            Email = visitor.Email.Value,
            Address = visitor.Address,
            ContactedAt = visitor.ContactedAt,
            ContactedByUserId = visitor.ContactedByUserId,
            CreatedAt = visitor.CreatedAt,
            UpdatedAt = visitor.UpdatedAt
        };
    }

    public static void ApplyChanges(Visitor visitor, VisitorPersistenceModel model)
    {
        model.ContactedAt = visitor.ContactedAt;
        model.ContactedByUserId = visitor.ContactedByUserId;
        model.UpdatedAt = visitor.UpdatedAt;
    }
}
