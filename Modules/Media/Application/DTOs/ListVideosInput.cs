using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class ListVideosInput
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;

    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
}
