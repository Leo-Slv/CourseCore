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

    public static RegisterInput ToInput(RegisterRequest request)
    {
        return new RegisterInput
        {
            Name = request.Name,
            Email = request.Email,
            Password = request.Password,
            CaptchaToken = request.CaptchaToken
        };
    }

    public static ConfirmEmailInput ToInput(Guid userId, ConfirmEmailRequest request)
    {
        return new ConfirmEmailInput
        {
            UserId = userId,
            Token = request.Token
        };
    }

    public static string ToRefreshToken(RefreshTokenRequest request)
    {
        return request.RefreshToken;
    }

    public static string ToRefreshToken(LogoutRequest request)
    {
        return request.RefreshToken;
    }

    public static AuthResponse ToResponse(AuthOutput output, bool exposeRefreshToken)
    {
        return new AuthResponse
        {
            UserId = output.UserId,
            Name = output.Name,
            Email = output.Email,
            Roles = output.Roles.ToList(),
            Token = ToResponse(output.Token, exposeRefreshToken)
        };
    }

    public static AuthTokenResponse ToResponse(AuthToken token, bool exposeRefreshToken)
    {
        return new AuthTokenResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = exposeRefreshToken ? token.RefreshToken : null,
            ExpiresAt = token.ExpiresAt
        };
    }
}
