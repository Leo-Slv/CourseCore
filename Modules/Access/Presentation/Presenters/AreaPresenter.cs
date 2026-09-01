using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;

namespace CourseCore.Api.Modules.Access.Presentation.Presenters;

public static class AreaPresenter
{
    public static CreateAreaInput ToInput(CreateAreaRequest request)
    {
        return new CreateAreaInput
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder
        };
    }

    public static UpdateAreaInput ToInput(Guid areaId, UpdateAreaRequest request)
    {
        return new UpdateAreaInput
        {
            AreaId = areaId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            Active = request.Active
        };
    }

    public static ListAreasInput ToInput(ListAreasRequest request)
    {
        return new ListAreasInput
        {
            Active = request.Active
        };
    }

    public static AreaResponse ToResponse(AreaOutput output)
    {
        return new AreaResponse
        {
            Id = output.Id,
            Name = output.Name,
            Slug = output.Slug,
            Description = output.Description,
            Active = output.Active,
            DisplayOrder = output.DisplayOrder,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static IReadOnlyCollection<AreaResponse> ToResponse(IReadOnlyCollection<AreaOutput> outputs)
    {
        return outputs.Select(ToResponse).ToList();
    }
}
