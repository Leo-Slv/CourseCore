using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Presentation.Presenters;
using CourseCore.Api.Modules.Media.Presentation.Requests;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Media.Presentation.Controllers;

[ApiController]
[Route("api/materials")]
[Authorize]
public class LessonMaterialsController : ControllerBase
{
    private readonly CreateLessonMaterialUseCase _createLessonMaterialUseCase;
    private readonly ListLessonMaterialsUseCase _listLessonMaterialsUseCase;
    private readonly UpdateLessonMaterialUseCase _updateLessonMaterialUseCase;
    private readonly RemoveLessonMaterialUseCase _removeLessonMaterialUseCase;
    private readonly ReorderLessonMaterialsUseCase _reorderLessonMaterialsUseCase;

    public LessonMaterialsController(
        CreateLessonMaterialUseCase createLessonMaterialUseCase,
        ListLessonMaterialsUseCase listLessonMaterialsUseCase,
        UpdateLessonMaterialUseCase updateLessonMaterialUseCase,
        RemoveLessonMaterialUseCase removeLessonMaterialUseCase,
        ReorderLessonMaterialsUseCase reorderLessonMaterialsUseCase)
    {
        _createLessonMaterialUseCase = createLessonMaterialUseCase;
        _listLessonMaterialsUseCase = listLessonMaterialsUseCase;
        _updateLessonMaterialUseCase = updateLessonMaterialUseCase;
        _removeLessonMaterialUseCase = removeLessonMaterialUseCase;
        _reorderLessonMaterialsUseCase = reorderLessonMaterialsUseCase;
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LessonMaterialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<LessonMaterialResponse>>> ListLessonMaterialsAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var output = await _listLessonMaterialsUseCase.ExecuteAsync(lessonId, cancellationToken);

        return Ok(LessonMaterialPresenter.ToResponse(output));
    }

    [HttpPost("lessons/{lessonId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(LessonMaterialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonMaterialResponse>> CreateLessonMaterialAsync(
        Guid lessonId,
        CreateLessonMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createLessonMaterialUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToInput(lessonId, request),
            cancellationToken);
        var response = LessonMaterialPresenter.ToResponse(output);

        return Created($"/api/materials/{response.Id}", response);
    }

    [HttpPut("{materialId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(LessonMaterialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonMaterialResponse>> UpdateLessonMaterialAsync(
        Guid materialId,
        UpdateLessonMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateLessonMaterialUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToInput(materialId, request),
            cancellationToken);

        return Ok(LessonMaterialPresenter.ToResponse(output));
    }

    [HttpDelete("{materialId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveLessonMaterialAsync(
        Guid materialId,
        CancellationToken cancellationToken)
    {
        await _removeLessonMaterialUseCase.ExecuteAsync(materialId, cancellationToken);

        return NoContent();
    }

    [HttpPatch("lessons/{lessonId:guid}/order")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReorderLessonMaterialsAsync(
        Guid lessonId,
        ReorderLessonMaterialsRequest request,
        CancellationToken cancellationToken)
    {
        await _reorderLessonMaterialsUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToInput(lessonId, request),
            cancellationToken);

        return NoContent();
    }
}
