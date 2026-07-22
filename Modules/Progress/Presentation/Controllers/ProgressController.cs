using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Presentation.Presenters;
using CourseCore.Api.Modules.Progress.Presentation.Requests;
using CourseCore.Api.Modules.Progress.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Progress.Presentation.Controllers;

[ApiController]
[Route("api/progress")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly RegisterLessonProgressUseCase _registerLessonProgressUseCase;
    private readonly GetCourseProgressUseCase _getCourseProgressUseCase;

    public ProgressController(
        RegisterLessonProgressUseCase registerLessonProgressUseCase,
        GetCourseProgressUseCase getCourseProgressUseCase)
    {
        _registerLessonProgressUseCase = registerLessonProgressUseCase;
        _getCourseProgressUseCase = getCourseProgressUseCase;
    }

    [HttpPost("lessons")]
    public async Task<ActionResult<LessonProgressResponse>> RegisterLessonProgressAsync(
        RegisterLessonProgressRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _registerLessonProgressUseCase.ExecuteAsync(
            ProgressPresenter.ToInput(request),
            cancellationToken);

        return Ok(ProgressPresenter.ToResponse(output));
    }

    [HttpPost("courses")]
    public async Task<ActionResult<CourseProgressResponse>> GetCourseProgressAsync(
        GetCourseProgressRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _getCourseProgressUseCase.ExecuteAsync(
            ProgressPresenter.ToInput(request),
            cancellationToken);

        return Ok(ProgressPresenter.ToResponse(output));
    }
}
