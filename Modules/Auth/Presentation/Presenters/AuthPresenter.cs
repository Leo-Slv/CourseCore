using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Presentation.Requests;
using CourseCore.Api.Modules.Auth.Presentation.Responses;

namespace CourseCore.Api.Modules.Auth.Presentation.Presenters;

public static class AuthPresenter
{
    public static LoginInput ToInput(LoginRequest request)
    {
        return new LoginInput
        {
            Email = request.Email,
            Password = request.Password
        };
    }

    public static string ToRefreshToken(RefreshTokenRequest request)
    {
        return request.RefreshToken;
    }

    public static AuthResponse ToResponse(AuthOutput output)
    {
        return new AuthResponse
        {
            UserId = output.UserId,
            Name = output.Name,
            Email = output.Email,
            Roles = output.Roles.ToList(),
            Token = ToResponse(output.Token)
        };
    }

    public static AuthTokenResponse ToResponse(AuthToken token)
    {
        return new AuthTokenResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.ExpiresAt
        };
    }
}
