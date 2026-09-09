using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Media.Presentation.Requests;

public sealed class ListVideosRequest
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;

    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
}
