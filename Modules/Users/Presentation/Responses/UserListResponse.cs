using CourseCore.Api.Shared.Presentation.Responses;

namespace CourseCore.Api.Modules.Users.Presentation.Responses;

public class UserListResponse
{
    public required PagedResponse<UserResponse> Page { get; init; }

    public required int TotalRegistered { get; init; }

    public required int TotalConfirmed { get; init; }
}
