using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Domain.Repositories;
using CourseCore.Api.Shared.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Visitors.Application.UseCases;

public class ListVisitorsUseCase
{
    private readonly IVisitorRepository _visitors;

    public ListVisitorsUseCase(IVisitorRepository visitors)
    {
        _visitors = visitors;
    }

    public async Task<PagedResult<VisitorOutput>> ExecuteAsync(
        ListVisitorsInput input,
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

        var search = string.IsNullOrWhiteSpace(input.Search) ? null : input.Search.Trim();
        var (visitors, totalCount) = await _visitors.ListPagedAsync(input.Page, input.PageSize, search, cancellationToken);

        return new PagedResult<VisitorOutput>
        {
            Items = visitors.Select(VisitorOutput.FromVisitor).ToList(),
            Page = input.Page,
            PageSize = input.PageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)input.PageSize)
        };
    }
}
