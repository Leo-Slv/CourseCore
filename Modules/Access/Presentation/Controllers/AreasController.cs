using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Presentation.Presenters;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Access.Presentation.Controllers;

[ApiController]
[Route("api/access")]
[Authorize]
public class AreasController : ControllerBase
{
    private readonly CheckCourseAccessUseCase _checkCourseAccessUseCase;
    private readonly ListUserCourseAccessUseCase _listUserCourseAccessUseCase;
    private readonly RevokeUserCourseAccessUseCase _revokeUserCourseAccessUseCase;
    private readonly ICurrentUserService _currentUserService;

    public AreasController(
        CheckCourseAccessUseCase checkCourseAccessUseCase,
        ListUserCourseAccessUseCase listUserCourseAccessUseCase,
        RevokeUserCourseAccessUseCase revokeUserCourseAccessUseCase,
        ICurrentUserService currentUserService)
    {
        _checkCourseAccessUseCase = checkCourseAccessUseCase;
        _listUserCourseAccessUseCase = listUserCourseAccessUseCase;
        _revokeUserCourseAccessUseCase = revokeUserCourseAccessUseCase;
        _currentUserService = currentUserService;
    }

    [HttpGet("user-course/{userId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageCourseAccess)]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserCourseAccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<UserCourseAccessResponse>>> ListUserCourseAccessAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var outputs = await _listUserCourseAccessUseCase.ExecuteAsync(userId, cancellationToken);

        return Ok(outputs.Select(AccessPresenter.ToResponse).ToList());
    }

    [HttpDelete("user-course/{userId:guid}/{courseId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageCourseAccess)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeUserCourseAccessAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        await _revokeUserCourseAccessUseCase.ExecuteAsync(userId, courseId, cancellationToken);

        return NoContent();
    }

    [HttpPost("course/check")]
    [Authorize(Policy = AuthPolicyNames.CheckOwnCourseAccess)]
    [Obsolete("Use GET /api/access/courses/{courseId} instead.")]
    [ProducesResponseType(typeof(CourseAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseAccessResponse>> CheckCourseAccessAsync(
        CheckCourseAccessRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _checkCourseAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(GetCurrentUserId(), request),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
    }

    [HttpGet("courses/{courseId:guid}")]
    [Authorize(Policy = AuthPolicyNames.CheckOwnCourseAccess)]
    [ProducesResponseType(typeof(CourseAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseAccessResponse>> CheckOwnCourseAccessAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var output = await _checkCourseAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(GetCurrentUserId(), courseId),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
    }

    [HttpGet("users/{userId:guid}/courses/{courseId:guid}")]
    [Authorize(Policy = AuthPolicyNames.CheckUserCourseAccess)]
    [ProducesResponseType(typeof(CourseAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseAccessResponse>> CheckUserCourseAccessAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var output = await _checkCourseAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(userId, courseId),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
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
