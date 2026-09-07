using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Presentation.Presenters;
using CourseCore.Api.Modules.Courses.Presentation.Requests;
using CourseCore.Api.Modules.Courses.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Courses.Presentation.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/modules/{moduleId:guid}/lessons")]
[Authorize(Policy = AuthPolicyNames.ManageCourses)]
public class LessonsController : ControllerBase
{
    private readonly CreateLessonUseCase _createLessonUseCase;
    private readonly UpdateLessonUseCase _updateLessonUseCase;
    private readonly RemoveLessonUseCase _removeLessonUseCase;
    private readonly ReorderLessonsUseCase _reorderLessonsUseCase;

    public LessonsController(
        CreateLessonUseCase createLessonUseCase,
        UpdateLessonUseCase updateLessonUseCase,
        RemoveLessonUseCase removeLessonUseCase,
        ReorderLessonsUseCase reorderLessonsUseCase)
    {
        _createLessonUseCase = createLessonUseCase;
        _updateLessonUseCase = updateLessonUseCase;
        _removeLessonUseCase = removeLessonUseCase;
        _reorderLessonsUseCase = reorderLessonsUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonResponse>> CreateAsync(
        Guid courseId,
        Guid moduleId,
        AddLessonRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createLessonUseCase.ExecuteAsync(
            CoursePresenter.ToInput(moduleId, request),
            cancellationToken);
        var response = CoursePresenter.ToResponse(output);

        return Created($"/api/courses/{courseId}/modules/{moduleId}/lessons/{response.Id}", response);
    }

    [HttpPut("{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonResponse>> UpdateAsync(
        Guid courseId,
        Guid moduleId,
        Guid lessonId,
        UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateLessonUseCase.ExecuteAsync(
            CoursePresenter.ToInput(lessonId, request),
            cancellationToken);

        return Ok(CoursePresenter.ToResponse(output));
    }

    [HttpDelete("{lessonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveAsync(
        Guid courseId,
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        await _removeLessonUseCase.ExecuteAsync(lessonId, cancellationToken);

        return NoContent();
    }

    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReorderAsync(
        Guid courseId,
        Guid moduleId,
        ReorderLessonsRequest request,
        CancellationToken cancellationToken)
    {
        await _reorderLessonsUseCase.ExecuteAsync(
            CoursePresenter.ToInput(moduleId, request),
            cancellationToken);

        return NoContent();
    }
}
