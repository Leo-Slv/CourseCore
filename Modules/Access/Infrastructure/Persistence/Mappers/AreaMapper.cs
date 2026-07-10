using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class AreaMapper
{
    public static Area ToDomain(AreaPersistenceModel model)
    {
        return Area.Restore(
            model.Id,
            model.Name,
            Slug.Create(model.Slug),
            model.Description,
            model.Active,
            model.DisplayOrder,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static AreaPersistenceModel ToPersistence(Area area)
    {
        return new AreaPersistenceModel
        {
            Id = area.Id,
            Name = area.Name,
            Slug = area.Slug.Value,
            Description = area.Description,
            Active = area.Active,
            DisplayOrder = area.DisplayOrder,
            CreatedAt = area.CreatedAt,
            UpdatedAt = area.UpdatedAt
        };
    }

    public static void ApplyChanges(Area area, AreaPersistenceModel model)
    {
        model.Name = area.Name;
        model.Slug = area.Slug.Value;
        model.Description = area.Description;
        model.Active = area.Active;
        model.DisplayOrder = area.DisplayOrder;
        model.UpdatedAt = area.UpdatedAt;
    }
}
