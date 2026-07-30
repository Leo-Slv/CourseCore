using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Auth.Presentation.Cookies;
using CourseCore.Api.Modules.Auth.Presentation.Presenters;
using CourseCore.Api.Modules.Auth.Presentation.Requests;
using CourseCore.Api.Modules.Auth.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RefreshTokenUseCase _refreshTokenUseCase;
    private readonly LogoutUseCase _logoutUseCase;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;
    private readonly AuthResponseOptions _authResponseOptions;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        LoginUseCase loginUseCase,
        RefreshTokenUseCase refreshTokenUseCase,
        LogoutUseCase logoutUseCase,
        IRefreshTokenCookieService refreshTokenCookieService,
        IOptions<AuthResponseOptions> authResponseOptions,
        IWebHostEnvironment environment)
    {
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _logoutUseCase = logoutUseCase;
        _refreshTokenCookieService = refreshTokenCookieService;
        _authResponseOptions = authResponseOptions.Value;
        _environment = environment;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _loginUseCase.ExecuteAsync(AuthPresenter.ToInput(request), cancellationToken);

        AppendRefreshTokenCookie(output);

        return Ok(AuthPresenter.ToResponse(output, ShouldExposeRefreshTokenInBody()));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRefresh)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenFromCookieOrBody(request.RefreshToken);
        var output = await _refreshTokenUseCase.ExecuteAsync(
            refreshToken,
            cancellationToken);

        AppendRefreshTokenCookie(output);

        return Ok(AuthPresenter.ToResponse(output, ShouldExposeRefreshTokenInBody()));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogout)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var refreshToken = GetRefreshTokenFromCookieOrBody(request.RefreshToken);

        await _logoutUseCase.ExecuteAsync(
            refreshToken,
            cancellationToken);

        _refreshTokenCookieService.Delete(Response);

        return NoContent();
    }

    private void AppendRefreshTokenCookie(AuthOutput output)
    {
        if (!string.IsNullOrWhiteSpace(output.Token.RefreshToken))
        {
            _refreshTokenCookieService.Append(Response, output.Token.RefreshToken);
        }
    }

    private string GetRefreshTokenFromCookieOrBody(string bodyRefreshToken)
    {
        var cookieRefreshToken = _refreshTokenCookieService.Read(Request);

        if (!string.IsNullOrWhiteSpace(cookieRefreshToken))
        {
            return cookieRefreshToken;
        }

        return _authResponseOptions.AllowRefreshTokenInBodyFallback
            ? bodyRefreshToken
            : string.Empty;
    }

    private bool ShouldExposeRefreshTokenInBody()
    {
        return !_environment.IsProduction() && _authResponseOptions.ExposeRefreshTokenInBody;
    }
}
