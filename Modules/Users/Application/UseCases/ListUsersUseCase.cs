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
    private readonly IAreaRepository _areas;

    public ListUsersUseCase(IUserRepository users, IRoleRepository roles, IAreaRepository areas)
    {
        _users = users;
        _roles = roles;
        _areas = areas;
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
        var userIds = users.Select(user => user.Id).ToList();
        var roleNamesByUserId = await _roles.FindRoleNamesByUserIdsAsync(userIds, cancellationToken);
        var areaNamesByUserId = await _areas.FindGrantedAreaNamesByUserIdsAsync(userIds, cancellationToken);
        var totalRegistered = await _users.CountAsync(cancellationToken);
        var totalConfirmed = await _users.CountConfirmedAsync(cancellationToken);

        var page = new PagedResult<UserOutput>
        {
            Items = users
                .Select(user => UserOutput.FromUser(
                    user,
                    roleNamesByUserId.TryGetValue(user.Id, out var roleNames) ? roleNames : null,
                    areaNamesByUserId.TryGetValue(user.Id, out var areaNames) ? areaNames : null))
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
