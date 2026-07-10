using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Presentation.Presenters;
using CourseCore.Api.Modules.Progress.Presentation.Requests;
using CourseCore.Api.Modules.Progress.Presentation.Responses;
using CourseCore.Api.Shared.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Progress.Presentation.Controllers;

[ApiController]
[Route("api/progress")]
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
        try
        {
            var output = await _registerLessonProgressUseCase.ExecuteAsync(
                ProgressPresenter.ToInput(request),
                cancellationToken);

            return Ok(ProgressPresenter.ToResponse(output));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("courses")]
    public async Task<ActionResult<CourseProgressResponse>> GetCourseProgressAsync(
        GetCourseProgressRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _getCourseProgressUseCase.ExecuteAsync(
                ProgressPresenter.ToInput(request),
                cancellationToken);

            return Ok(ProgressPresenter.ToResponse(output));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
