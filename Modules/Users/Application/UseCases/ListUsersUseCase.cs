using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class ListUsersUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public ListUsersUseCase(IUserRepository users, IRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<UserListOutput> ExecuteAsync(
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

        var search = string.IsNullOrWhiteSpace(input.Search) ? null : input.Search.Trim();
        var (users, totalCount) = await _users.ListPagedAsync(input.Page, input.PageSize, search, cancellationToken);
        var roleNamesByUserId = await _roles.FindRoleNamesByUserIdsAsync(
            users.Select(user => user.Id).ToList(),
            cancellationToken);
        var totalRegistered = await _users.CountAsync(cancellationToken);
        var totalConfirmed = await _users.CountConfirmedAsync(cancellationToken);

        var page = new PagedResult<UserOutput>
        {
            Items = users
                .Select(user => UserOutput.FromUser(
                    user,
                    roleNamesByUserId.TryGetValue(user.Id, out var roleNames) ? roleNames : null))
                .ToList(),
            Page = input.Page,
            PageSize = input.PageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)input.PageSize)
        };

        return new UserListOutput
        {
            Page = page,
            TotalRegistered = totalRegistered,
            TotalConfirmed = totalConfirmed
        };
    }
}
