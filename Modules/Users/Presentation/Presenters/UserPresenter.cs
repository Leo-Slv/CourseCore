using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Presentation.Requests;
using CourseCore.Api.Modules.Users.Presentation.Responses;

namespace CourseCore.Api.Modules.Users.Presentation.Presenters;

public static class UserPresenter
{
    public static CreateUserInput ToInput(CreateUserRequest request)
    {
        return new CreateUserInput
        {
            Name = request.Name,
            Email = request.Email,
            Password = request.Password
        };
    }

    public static UpdateUserInput ToInput(Guid userId, UpdateUserRequest request)
    {
        return new UpdateUserInput
        {
            UserId = userId,
            Name = request.Name,
            Email = request.Email,
            Active = request.Active
        };
    }

    public static UserResponse ToResponse(UserOutput output)
    {
        return new UserResponse
        {
            Id = output.Id,
            Name = output.Name,
            Email = output.Email,
            Active = output.Active,
            EmailVerifiedAt = output.EmailVerifiedAt,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static IReadOnlyCollection<UserResponse> ToResponse(IReadOnlyCollection<UserOutput> outputs)
    {
        return outputs.Select(ToResponse).ToList();
    }
}
