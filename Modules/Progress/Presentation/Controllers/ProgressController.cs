using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Presentation.Presenters;
using CourseCore.Api.Modules.Progress.Presentation.Requests;
using CourseCore.Api.Modules.Progress.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
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
    private readonly ICurrentUserService _currentUserService;

    public ProgressController(
        RegisterLessonProgressUseCase registerLessonProgressUseCase,
        GetCourseProgressUseCase getCourseProgressUseCase,
        ICurrentUserService currentUserService)
    {
        _registerLessonProgressUseCase = registerLessonProgressUseCase;
        _getCourseProgressUseCase = getCourseProgressUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost("lessons")]
    [ProducesResponseType(typeof(LessonProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonProgressResponse>> RegisterLessonProgressAsync(
        RegisterLessonProgressRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _registerLessonProgressUseCase.ExecuteAsync(
            ProgressPresenter.ToInput(GetCurrentUserId(), request),
            cancellationToken);

        return Ok(ProgressPresenter.ToResponse(output));
    }

    [HttpPost("courses")]
    [ProducesResponseType(typeof(CourseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseProgressResponse>> GetCourseProgressAsync(
        GetCourseProgressRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _getCourseProgressUseCase.ExecuteAsync(
            ProgressPresenter.ToInput(GetCurrentUserId(), request),
            cancellationToken);

        return Ok(ProgressPresenter.ToResponse(output));
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
