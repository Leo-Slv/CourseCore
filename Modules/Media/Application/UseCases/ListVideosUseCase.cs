using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class ListVideosUseCase
{
    private readonly IVideoRepository _videos;

    public ListVideosUseCase(IVideoRepository videos)
    {
        _videos = videos;
    }

    public async Task<PagedResult<VideoOutput>> ExecuteAsync(
        ListVideosInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.Page < 1)
        {
            throw new ApplicationValidationException("Page must be greater than or equal to 1.");
        }

        if (input.PageSize < 1 || input.PageSize > PaginationLimits.MaximumPageSize)
        {
            throw new ApplicationValidationException(
                $"PageSize must be between 1 and {PaginationLimits.MaximumPageSize}.");
        }

        var (items, totalCount) = await _videos.ListPagedAsync(input.Page, input.PageSize, cancellationToken);

        return new PagedResult<VideoOutput>
        {
            Items = items.Select(VideoOutput.FromVideo).ToList(),
            Page = input.Page,
            PageSize = input.PageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)input.PageSize)
        };
    }
}
