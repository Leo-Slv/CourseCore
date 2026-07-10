using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class RefreshTokenUseCase
{
    private readonly ITokenService _tokenService;

    public RefreshTokenUseCase(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<AuthOutput> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var userId = await _tokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);

        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        throw new NotSupportedException("Refresh token reissue requires persistent refresh token storage.");
    }
}
