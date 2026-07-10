using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Presentation.Presenters;
using CourseCore.Api.Modules.Auth.Presentation.Requests;
using CourseCore.Api.Modules.Auth.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Auth.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RefreshTokenUseCase _refreshTokenUseCase;

    public AuthController(
        LoginUseCase loginUseCase,
        RefreshTokenUseCase refreshTokenUseCase)
    {
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _loginUseCase.ExecuteAsync(AuthPresenter.ToInput(request), cancellationToken);

        return Ok(AuthPresenter.ToResponse(output));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _refreshTokenUseCase.ExecuteAsync(
            AuthPresenter.ToRefreshToken(request),
            cancellationToken);

        return Ok(AuthPresenter.ToResponse(output));
    }
}
