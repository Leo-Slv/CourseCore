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
    private readonly GrantUserAreaAccessUseCase _grantUserAreaAccessUseCase;
    private readonly GrantRoleAreaAccessUseCase _grantRoleAreaAccessUseCase;
    private readonly CheckCourseAccessUseCase _checkCourseAccessUseCase;
    private readonly ICurrentUserService _currentUserService;

    public AreasController(
        GrantUserAreaAccessUseCase grantUserAreaAccessUseCase,
        GrantRoleAreaAccessUseCase grantRoleAreaAccessUseCase,
        CheckCourseAccessUseCase checkCourseAccessUseCase,
        ICurrentUserService currentUserService)
    {
        _grantUserAreaAccessUseCase = grantUserAreaAccessUseCase;
        _grantRoleAreaAccessUseCase = grantRoleAreaAccessUseCase;
        _checkCourseAccessUseCase = checkCourseAccessUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost("user-area")]
    [Authorize(Policy = AuthPolicyNames.ManageUserAreaAccess)]
    [ProducesResponseType(typeof(AreaAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AreaAccessResponse>> GrantUserAreaAccessAsync(
        GrantUserAreaAccessRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _grantUserAreaAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(request),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
    }

    [HttpPost("role-area")]
    [Authorize(Policy = AuthPolicyNames.ManageRoleAreaAccess)]
    [ProducesResponseType(typeof(AreaAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AreaAccessResponse>> GrantRoleAreaAccessAsync(
        GrantRoleAreaAccessRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _grantRoleAreaAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(request),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
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
