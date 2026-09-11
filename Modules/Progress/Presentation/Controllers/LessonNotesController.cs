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
[Route("api/notes")]
[Authorize]
public class LessonNotesController : ControllerBase
{
    private readonly UpsertLessonNoteUseCase _upsertLessonNoteUseCase;
    private readonly GetLessonNoteUseCase _getLessonNoteUseCase;
    private readonly RemoveLessonNoteUseCase _removeLessonNoteUseCase;
    private readonly ICurrentUserService _currentUserService;

    public LessonNotesController(
        UpsertLessonNoteUseCase upsertLessonNoteUseCase,
        GetLessonNoteUseCase getLessonNoteUseCase,
        RemoveLessonNoteUseCase removeLessonNoteUseCase,
        ICurrentUserService currentUserService)
    {
        _upsertLessonNoteUseCase = upsertLessonNoteUseCase;
        _getLessonNoteUseCase = getLessonNoteUseCase;
        _removeLessonNoteUseCase = removeLessonNoteUseCase;
        _currentUserService = currentUserService;
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonNoteResponse>> GetLessonNoteAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var output = await _getLessonNoteUseCase.ExecuteAsync(GetCurrentUserId(), lessonId, cancellationToken);

        return Ok(ProgressPresenter.ToResponse(output));
    }

    [HttpPut("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonNoteResponse>> UpsertLessonNoteAsync(
        Guid lessonId,
        UpsertLessonNoteRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _upsertLessonNoteUseCase.ExecuteAsync(
            ProgressPresenter.ToInput(GetCurrentUserId(), lessonId, request),
            cancellationToken);

        return Ok(ProgressPresenter.ToResponse(output));
    }

    [HttpDelete("lessons/{lessonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveLessonNoteAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        await _removeLessonNoteUseCase.ExecuteAsync(GetCurrentUserId(), lessonId, cancellationToken);

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
