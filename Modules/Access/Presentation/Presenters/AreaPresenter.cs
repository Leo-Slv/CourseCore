using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Media.Application.Services;

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
            DisplayOrder = request.DisplayOrder,
            AccentColor = request.AccentColor,
            ImageUrl = request.ImageUrl
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
            Active = request.Active,
            AccentColor = request.AccentColor,
            ImageUrl = request.ImageUrl
        };
    }

    public static ListAreasInput ToInput(ListAreasRequest request)
    {
        return new ListAreasInput
        {
            Active = request.Active
        };
    }

    public static async Task<AreaResponse> ToResponseAsync(
        AreaOutput output,
        ImageUrlResolver imageUrlResolver,
        CancellationToken cancellationToken = default)
    {
        return new AreaResponse
        {
            Id = output.Id,
            Name = output.Name,
            Slug = output.Slug,
            Description = output.Description,
            Active = output.Active,
            DisplayOrder = output.DisplayOrder,
            AccentColor = output.AccentColor,
            ImageUrl = await imageUrlResolver.ResolveAsync(output.ImageUrl, cancellationToken),
            CourseCount = output.CourseCount,
            Courses = output.Courses.Select(ToResponse).ToList(),
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static AreaCourseSummaryResponse ToResponse(AreaCourseSummaryOutput output)
    {
        return new AreaCourseSummaryResponse
        {
            Id = output.Id,
            Title = output.Title,
            Slug = output.Slug,
            Published = output.Published
        };
    }

    public static async Task<IReadOnlyCollection<AreaResponse>> ToResponseAsync(
        IReadOnlyCollection<AreaOutput> outputs,
        ImageUrlResolver imageUrlResolver,
        CancellationToken cancellationToken = default)
    {
        var responses = new List<AreaResponse>();
        foreach (var output in outputs)
        {
            responses.Add(await ToResponseAsync(output, imageUrlResolver, cancellationToken));
        }

        return responses;
    }
}
