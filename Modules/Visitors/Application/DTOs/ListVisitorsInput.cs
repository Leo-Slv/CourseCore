using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Visitors.Application.DTOs;

public sealed class ListVisitorsInput
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;
    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
    public string? Search { get; init; }
}
