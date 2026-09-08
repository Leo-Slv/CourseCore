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
[Route("api/courses/{courseId:guid}/modules")]
[Authorize(Policy = AuthPolicyNames.ManageCourses)]
public class CourseModulesController : ControllerBase
{
    private readonly CreateCourseModuleUseCase _createCourseModuleUseCase;
    private readonly UpdateCourseModuleUseCase _updateCourseModuleUseCase;
    private readonly RemoveCourseModuleUseCase _removeCourseModuleUseCase;
    private readonly ReorderCourseModulesUseCase _reorderCourseModulesUseCase;
    private readonly ListCourseModulesUseCase _listCourseModulesUseCase;

    public CourseModulesController(
        CreateCourseModuleUseCase createCourseModuleUseCase,
        UpdateCourseModuleUseCase updateCourseModuleUseCase,
        RemoveCourseModuleUseCase removeCourseModuleUseCase,
        ReorderCourseModulesUseCase reorderCourseModulesUseCase,
        ListCourseModulesUseCase listCourseModulesUseCase)
    {
        _createCourseModuleUseCase = createCourseModuleUseCase;
        _updateCourseModuleUseCase = updateCourseModuleUseCase;
        _removeCourseModuleUseCase = removeCourseModuleUseCase;
        _reorderCourseModulesUseCase = reorderCourseModulesUseCase;
        _listCourseModulesUseCase = listCourseModulesUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CourseModuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<CourseModuleResponse>>> ListAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var outputs = await _listCourseModulesUseCase.ExecuteAsync(courseId, cancellationToken);

        return Ok(outputs.Select(CoursePresenter.ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseModuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseModuleResponse>> CreateAsync(
        Guid courseId,
        AddCourseModuleRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createCourseModuleUseCase.ExecuteAsync(
            CoursePresenter.ToInput(courseId, request),
            cancellationToken);
        var response = CoursePresenter.ToResponse(output);

        return Created($"/api/courses/{courseId}/modules/{response.Id}", response);
    }

    [HttpPut("{moduleId:guid}")]
    [ProducesResponseType(typeof(CourseModuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseModuleResponse>> UpdateAsync(
        Guid courseId,
        Guid moduleId,
        UpdateCourseModuleRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateCourseModuleUseCase.ExecuteAsync(
            CoursePresenter.ToInput(moduleId, request),
            cancellationToken);

        return Ok(CoursePresenter.ToResponse(output));
    }

    [HttpDelete("{moduleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        await _removeCourseModuleUseCase.ExecuteAsync(moduleId, cancellationToken);

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
        ReorderCourseModulesRequest request,
        CancellationToken cancellationToken)
    {
        await _reorderCourseModulesUseCase.ExecuteAsync(
            CoursePresenter.ToInput(courseId, request),
            cancellationToken);

        return NoContent();
    }
}
