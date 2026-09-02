using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Auth.Presentation.Cookies;
using CourseCore.Api.Modules.Auth.Presentation.Presenters;
using CourseCore.Api.Modules.Auth.Presentation.Requests;
using CourseCore.Api.Modules.Auth.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
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
    private readonly RegisterUseCase _registerUseCase;
    private readonly ConfirmEmailUseCase _confirmEmailUseCase;
    private readonly ResendEmailConfirmationUseCase _resendEmailConfirmationUseCase;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;
    private readonly ICurrentUserService _currentUserService;
    private readonly AuthResponseOptions _authResponseOptions;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        LoginUseCase loginUseCase,
        RefreshTokenUseCase refreshTokenUseCase,
        LogoutUseCase logoutUseCase,
        RegisterUseCase registerUseCase,
        ConfirmEmailUseCase confirmEmailUseCase,
        ResendEmailConfirmationUseCase resendEmailConfirmationUseCase,
        IRefreshTokenCookieService refreshTokenCookieService,
        ICurrentUserService currentUserService,
        IOptions<AuthResponseOptions> authResponseOptions,
        IWebHostEnvironment environment)
    {
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _logoutUseCase = logoutUseCase;
        _registerUseCase = registerUseCase;
        _confirmEmailUseCase = confirmEmailUseCase;
        _resendEmailConfirmationUseCase = resendEmailConfirmationUseCase;
        _refreshTokenCookieService = refreshTokenCookieService;
        _currentUserService = currentUserService;
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

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRegister)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _registerUseCase.ExecuteAsync(AuthPresenter.ToInput(request), cancellationToken);

        AppendRefreshTokenCookie(output);

        return StatusCode(
            StatusCodes.Status201Created,
            AuthPresenter.ToResponse(output, ShouldExposeRefreshTokenInBody()));
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        await _confirmEmailUseCase.ExecuteAsync(
            AuthPresenter.ToInput(GetCurrentUserId(), request),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthResendConfirmation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResendConfirmationAsync(CancellationToken cancellationToken)
    {
        await _resendEmailConfirmationUseCase.ExecuteAsync(GetCurrentUserId(), cancellationToken);

        return NoContent();
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

    private Guid GetCurrentUserId()
    {
        var userId = _currentUserService.UserId;

        if (userId is null || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authenticated user was not found.");
        }

        return userId.Value;
    }
}
