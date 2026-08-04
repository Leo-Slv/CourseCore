using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class ListUsersUseCase
{
    private readonly IUserRepository _users;

    public ListUsersUseCase(IUserRepository users)
    {
        _users = users;
    }

    public async Task<PagedResult<UserOutput>> ExecuteAsync(
        ListUsersInput input,
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

        var (users, totalCount) = await _users.ListPagedAsync(input.Page, input.PageSize, cancellationToken);

        return new PagedResult<UserOutput>
        {
            Items = users.Select(UserOutput.FromUser).ToList(),
            Page = input.Page,
            PageSize = input.PageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)input.PageSize)
        };
    }
}
