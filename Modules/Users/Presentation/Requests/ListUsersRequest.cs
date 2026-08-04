using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.Users.Presentation.Requests;

public sealed class ListUsersRequest
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;
    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
}
