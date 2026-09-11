using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Questions.Application.UseCases;
using CourseCore.Api.Modules.Questions.Presentation.Presenters;
using CourseCore.Api.Modules.Questions.Presentation.Requests;
using CourseCore.Api.Modules.Questions.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Questions.Presentation.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize]
public class LessonQuestionsController : ControllerBase
{
    private readonly AskLessonQuestionUseCase _askLessonQuestionUseCase;
    private readonly ListLessonQuestionsUseCase _listLessonQuestionsUseCase;
    private readonly AnswerLessonQuestionUseCase _answerLessonQuestionUseCase;
    private readonly RemoveLessonQuestionUseCase _removeLessonQuestionUseCase;
    private readonly ICurrentUserService _currentUserService;

    public LessonQuestionsController(
        AskLessonQuestionUseCase askLessonQuestionUseCase,
        ListLessonQuestionsUseCase listLessonQuestionsUseCase,
        AnswerLessonQuestionUseCase answerLessonQuestionUseCase,
        RemoveLessonQuestionUseCase removeLessonQuestionUseCase,
        ICurrentUserService currentUserService)
    {
        _askLessonQuestionUseCase = askLessonQuestionUseCase;
        _listLessonQuestionsUseCase = listLessonQuestionsUseCase;
        _answerLessonQuestionUseCase = answerLessonQuestionUseCase;
        _removeLessonQuestionUseCase = removeLessonQuestionUseCase;
        _currentUserService = currentUserService;
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LessonQuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<LessonQuestionResponse>>> ListLessonQuestionsAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var outputs = await _listLessonQuestionsUseCase.ExecuteAsync(GetCurrentUserId(), lessonId, cancellationToken);

        return Ok(outputs.Select(LessonQuestionPresenter.ToResponse).ToList());
    }

    [HttpPost("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonQuestionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonQuestionResponse>> AskLessonQuestionAsync(
        Guid lessonId,
        AskLessonQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _askLessonQuestionUseCase.ExecuteAsync(
            LessonQuestionPresenter.ToInput(GetCurrentUserId(), lessonId, request),
            cancellationToken);
        var response = LessonQuestionPresenter.ToResponse(output);

        return Created($"/api/questions/{response.Id}", response);
    }

    [HttpPost("{questionId:guid}/answer")]
    [Authorize(Policy = AuthPolicyNames.ManageCourses)]
    [ProducesResponseType(typeof(LessonQuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonQuestionResponse>> AnswerLessonQuestionAsync(
        Guid questionId,
        AnswerLessonQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _answerLessonQuestionUseCase.ExecuteAsync(
            LessonQuestionPresenter.ToInput(questionId, GetCurrentUserId(), request),
            cancellationToken);

        return Ok(LessonQuestionPresenter.ToResponse(output));
    }

    [HttpDelete("{questionId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageCourses)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveLessonQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken)
    {
        await _removeLessonQuestionUseCase.ExecuteAsync(questionId, cancellationToken);

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
