using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Presentation.Presenters;
using CourseCore.Api.Modules.Media.Presentation.Requests;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Media.Presentation.Controllers;

[ApiController]
[Route("api/videos")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly CreateVideoUseCase _createVideoUseCase;
    private readonly MarkVideoReadyUseCase _markVideoReadyUseCase;
    private readonly RequestVideoPlaybackUseCase _requestVideoPlaybackUseCase;
    private readonly ICurrentUserService _currentUserService;

    public VideosController(
        CreateVideoUseCase createVideoUseCase,
        MarkVideoReadyUseCase markVideoReadyUseCase,
        RequestVideoPlaybackUseCase requestVideoPlaybackUseCase,
        ICurrentUserService currentUserService)
    {
        _createVideoUseCase = createVideoUseCase;
        _markVideoReadyUseCase = markVideoReadyUseCase;
        _requestVideoPlaybackUseCase = requestVideoPlaybackUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> CreateAsync(
        CreateVideoRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createVideoUseCase.ExecuteAsync(
            VideoPresenter.ToInput(request),
            cancellationToken);
        var response = VideoPresenter.ToResponse(output);

        return Created($"/api/videos/{response.Id}", response);
    }

    [HttpPost("{id:guid}/ready")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> MarkReadyAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var output = await _markVideoReadyUseCase.ExecuteAsync(
            new MarkVideoReadyInput { VideoId = id },
            cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpPost("playback")]
    [ProducesResponseType(typeof(VideoPlaybackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoPlaybackResponse>> RequestPlaybackAsync(
        RequestVideoPlaybackRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _requestVideoPlaybackUseCase.ExecuteAsync(
            VideoPresenter.ToInput(GetCurrentUserId(), request),
            cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
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
