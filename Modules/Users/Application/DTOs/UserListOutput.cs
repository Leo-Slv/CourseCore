using CourseCore.Api.Shared.Application.DTOs;

namespace CourseCore.Api.Modules.Users.Application.DTOs;

public sealed class UserListOutput
{
    public required PagedResult<UserOutput> Page { get; init; }

    public required int TotalRegistered { get; init; }

    public required int TotalConfirmed { get; init; }
}
