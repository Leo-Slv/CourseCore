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
    private readonly GetLessonVideoUseCase _getLessonVideoUseCase;
    private readonly ReplaceLessonVideoUseCase _replaceLessonVideoUseCase;
    private readonly RemoveLessonVideoUseCase _removeLessonVideoUseCase;
    private readonly ListVideosUseCase _listVideosUseCase;
    private readonly ActivateVideoUseCase _activateVideoUseCase;
    private readonly UnlistVideoUseCase _unlistVideoUseCase;
    private readonly GetYouTubeVideoMetadataUseCase _getYouTubeVideoMetadataUseCase;
    private readonly ICurrentUserService _currentUserService;

    public VideosController(
        CreateVideoUseCase createVideoUseCase,
        MarkVideoReadyUseCase markVideoReadyUseCase,
        RequestVideoPlaybackUseCase requestVideoPlaybackUseCase,
        GetLessonVideoUseCase getLessonVideoUseCase,
        ReplaceLessonVideoUseCase replaceLessonVideoUseCase,
        RemoveLessonVideoUseCase removeLessonVideoUseCase,
        ListVideosUseCase listVideosUseCase,
        ActivateVideoUseCase activateVideoUseCase,
        UnlistVideoUseCase unlistVideoUseCase,
        GetYouTubeVideoMetadataUseCase getYouTubeVideoMetadataUseCase,
        ICurrentUserService currentUserService)
    {
        _createVideoUseCase = createVideoUseCase;
        _markVideoReadyUseCase = markVideoReadyUseCase;
        _requestVideoPlaybackUseCase = requestVideoPlaybackUseCase;
        _getLessonVideoUseCase = getLessonVideoUseCase;
        _replaceLessonVideoUseCase = replaceLessonVideoUseCase;
        _removeLessonVideoUseCase = removeLessonVideoUseCase;
        _listVideosUseCase = listVideosUseCase;
        _activateVideoUseCase = activateVideoUseCase;
        _unlistVideoUseCase = unlistVideoUseCase;
        _getYouTubeVideoMetadataUseCase = getYouTubeVideoMetadataUseCase;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(PagedResponse<VideoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<VideoResponse>>> ListAsync(
        [FromQuery] ListVideosRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _listVideosUseCase.ExecuteAsync(VideoPresenter.ToInput(request), cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpGet("youtube-metadata")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(YouTubeVideoMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<YouTubeVideoMetadataResponse>> GetYouTubeMetadataAsync(
        [FromQuery] string videoId,
        CancellationToken cancellationToken)
    {
        var output = await _getYouTubeVideoMetadataUseCase.ExecuteAsync(videoId, cancellationToken);

        return Ok(new YouTubeVideoMetadataResponse
        {
            Title = output.Title,
            ThumbnailUrl = output.ThumbnailUrl,
            DurationSeconds = output.DurationSeconds
        });
    }

    [HttpPost("{videoId:guid}/activate")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> ActivateAsync(
        Guid videoId,
        CancellationToken cancellationToken)
    {
        var output = await _activateVideoUseCase.ExecuteAsync(videoId, cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpPost("{videoId:guid}/unlist")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> UnlistAsync(
        Guid videoId,
        CancellationToken cancellationToken)
    {
        var output = await _unlistVideoUseCase.ExecuteAsync(videoId, cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
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

    [HttpGet("{videoId:guid}/playback")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(VideoPlaybackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoPlaybackResponse>> RequestPlaybackAsync(
        Guid videoId,
        CancellationToken cancellationToken)
    {
        var output = await _requestVideoPlaybackUseCase.ExecuteAsync(
            new RequestVideoPlaybackInput { UserId = GetCurrentUserId(), VideoId = videoId },
            cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> GetLessonVideoAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var output = await _getLessonVideoUseCase.ExecuteAsync(lessonId, cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpPut("lessons/{lessonId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(VideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoResponse>> ReplaceLessonVideoAsync(
        Guid lessonId,
        ReplaceLessonVideoRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _replaceLessonVideoUseCase.ExecuteAsync(
            VideoPresenter.ToInput(lessonId, request),
            cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpDelete("lessons/{lessonId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveLessonVideoAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        await _removeLessonVideoUseCase.ExecuteAsync(lessonId, cancellationToken);

        return NoContent();
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
