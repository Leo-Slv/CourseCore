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
[Route("api/access/requests")]
[Authorize]
public class AccessRequestsController : ControllerBase
{
    private readonly RequestCourseAccessUseCase _requestCourseAccessUseCase;
    private readonly ListMyAccessRequestsUseCase _listMyAccessRequestsUseCase;
    private readonly ListAccessRequestsUseCase _listAccessRequestsUseCase;
    private readonly ApproveAccessRequestUseCase _approveAccessRequestUseCase;
    private readonly RejectAccessRequestUseCase _rejectAccessRequestUseCase;
    private readonly ICurrentUserService _currentUserService;

    public AccessRequestsController(
        RequestCourseAccessUseCase requestCourseAccessUseCase,
        ListMyAccessRequestsUseCase listMyAccessRequestsUseCase,
        ListAccessRequestsUseCase listAccessRequestsUseCase,
        ApproveAccessRequestUseCase approveAccessRequestUseCase,
        RejectAccessRequestUseCase rejectAccessRequestUseCase,
        ICurrentUserService currentUserService)
    {
        _requestCourseAccessUseCase = requestCourseAccessUseCase;
        _listMyAccessRequestsUseCase = listMyAccessRequestsUseCase;
        _listAccessRequestsUseCase = listAccessRequestsUseCase;
        _approveAccessRequestUseCase = approveAccessRequestUseCase;
        _rejectAccessRequestUseCase = rejectAccessRequestUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(AccessRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AccessRequestResponse>> RequestCourseAccessAsync(
        RequestCourseAccessRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _requestCourseAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(GetCurrentUserId(), request),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AccessRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<AccessRequestResponse>>> ListMineAsync(
        CancellationToken cancellationToken)
    {
        var outputs = await _listMyAccessRequestsUseCase.ExecuteAsync(
            AccessPresenter.ToListMyAccessRequestsInput(GetCurrentUserId()),
            cancellationToken);

        return Ok(outputs.Select(AccessPresenter.ToResponse).ToList());
    }

    [HttpGet("")]
    [Authorize(Policy = AuthPolicyNames.ManageUserAreaAccess)]
    [ProducesResponseType(typeof(IReadOnlyCollection<AccessRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<AccessRequestResponse>>> ListAsync(
        [FromQuery] ListAccessRequestsRequest request,
        CancellationToken cancellationToken)
    {
        var outputs = await _listAccessRequestsUseCase.ExecuteAsync(
            AccessPresenter.ToInput(request),
            cancellationToken);

        return Ok(outputs.Select(AccessPresenter.ToResponse).ToList());
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicyNames.ManageUserAreaAccess)]
    [ProducesResponseType(typeof(AccessRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AccessRequestResponse>> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var output = await _approveAccessRequestUseCase.ExecuteAsync(
            AccessPresenter.ToApproveInput(id, GetCurrentUserId()),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicyNames.ManageUserAreaAccess)]
    [ProducesResponseType(typeof(AccessRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AccessRequestResponse>> RejectAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var output = await _rejectAccessRequestUseCase.ExecuteAsync(
            AccessPresenter.ToRejectInput(id, GetCurrentUserId()),
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
