using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Users.Application.DTOs;

public sealed class ListUsersInput
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;
    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
}
