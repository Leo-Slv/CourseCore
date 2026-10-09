using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Visitors.Application.UseCases;
using CourseCore.Api.Modules.Visitors.Presentation.Presenters;
using CourseCore.Api.Modules.Visitors.Presentation.Requests;
using CourseCore.Api.Modules.Visitors.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.RateLimiting;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CourseCore.Api.Modules.Visitors.Presentation.Controllers;

[ApiController]
[Route("api/visitors")]
[Authorize]
public class VisitorsController : ControllerBase
{
    private readonly RegisterVisitorUseCase _registerVisitorUseCase;
    private readonly ListVisitorsUseCase _listVisitorsUseCase;
    private readonly SetVisitorContactedUseCase _setVisitorContactedUseCase;
    private readonly ICurrentUserService _currentUserService;

    public VisitorsController(
        RegisterVisitorUseCase registerVisitorUseCase,
        ListVisitorsUseCase listVisitorsUseCase,
        SetVisitorContactedUseCase setVisitorContactedUseCase,
        ICurrentUserService currentUserService)
    {
        _registerVisitorUseCase = registerVisitorUseCase;
        _listVisitorsUseCase = listVisitorsUseCase;
        _setVisitorContactedUseCase = setVisitorContactedUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.VisitorRegistration)]
    [ProducesResponseType(typeof(RegisterVisitorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterVisitorResponse>> RegisterAsync(
        RegisterVisitorRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _registerVisitorUseCase.ExecuteAsync(VisitorPresenter.ToInput(request), cancellationToken);

        // There is no single-visitor read endpoint, so no Location header is emitted.
        return StatusCode(StatusCodes.Status201Created, VisitorPresenter.ToResponse(output));
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicyNames.ReadVisitors)]
    [ProducesResponseType(typeof(PagedResponse<VisitorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<VisitorResponse>>> ListAsync(
        [FromQuery] ListVisitorsRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _listVisitorsUseCase.ExecuteAsync(VisitorPresenter.ToInput(request), cancellationToken);

        return Ok(VisitorPresenter.ToResponse(output));
    }

    [HttpPut("{id:guid}/contacted")]
    [Authorize(Policy = AuthPolicyNames.ManageVisitors)]
    [ProducesResponseType(typeof(VisitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VisitorResponse>> SetContactedAsync(
        Guid id,
        SetVisitorContactedRequest request,
        CancellationToken cancellationToken)
    {
        var actingUserId = _currentUserService.UserId;

        if (actingUserId is null || actingUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authenticated user was not found.");
        }

        var output = await _setVisitorContactedUseCase.ExecuteAsync(
            id,
            request.Contacted,
            actingUserId.Value,
            cancellationToken);

        return Ok(VisitorPresenter.ToResponse(output));
    }
}
