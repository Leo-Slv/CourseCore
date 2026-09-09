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

    public static RequestPasswordResetInput ToInput(RequestPasswordResetRequest request)
    {
        return new RequestPasswordResetInput
        {
            Email = request.Email,
            CaptchaToken = request.CaptchaToken
        };
    }

    public static ConfirmPasswordResetInput ToInput(ConfirmPasswordResetRequest request)
    {
        return new ConfirmPasswordResetInput
        {
            Token = request.Token,
            NewPassword = request.NewPassword
        };
    }

    public static UpdateOwnProfileInput ToInput(Guid userId, UpdateProfileRequest request)
    {
        return new UpdateOwnProfileInput
        {
            UserId = userId,
            Name = request.Name,
            Phone = request.Phone,
            AvatarUrl = request.AvatarUrl
        };
    }

    public static ChangeOwnPasswordInput ToInput(Guid userId, ChangePasswordRequest request)
    {
        return new ChangeOwnPasswordInput
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
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

    public static CurrentUserResponse ToResponse(CurrentUserOutput output)
    {
        return new CurrentUserResponse
        {
            UserId = output.UserId,
            Name = output.Name,
            Email = output.Email,
            Active = output.Active,
            EmailVerifiedAt = output.EmailVerifiedAt,
            Phone = output.Phone,
            AvatarUrl = output.AvatarUrl,
            Roles = output.Roles.ToList()
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
