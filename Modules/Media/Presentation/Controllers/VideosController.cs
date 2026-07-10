using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Presentation.Presenters;
using CourseCore.Api.Modules.Media.Presentation.Requests;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Media.Presentation.Controllers;

[ApiController]
[Route("api/videos")]
public class VideosController : ControllerBase
{
    private readonly CreateVideoUseCase _createVideoUseCase;
    private readonly RequestVideoPlaybackUseCase _requestVideoPlaybackUseCase;

    public VideosController(
        CreateVideoUseCase createVideoUseCase,
        RequestVideoPlaybackUseCase requestVideoPlaybackUseCase)
    {
        _createVideoUseCase = createVideoUseCase;
        _requestVideoPlaybackUseCase = requestVideoPlaybackUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<VideoResponse>> CreateAsync(
        CreateVideoRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createVideoUseCase.ExecuteAsync(
            VideoPresenter.ToInput(request),
            cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }

    [HttpPost("playback")]
    public async Task<ActionResult<VideoPlaybackResponse>> RequestPlaybackAsync(
        RequestVideoPlaybackRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _requestVideoPlaybackUseCase.ExecuteAsync(
            VideoPresenter.ToInput(request),
            cancellationToken);

        return Ok(VideoPresenter.ToResponse(output));
    }
}
