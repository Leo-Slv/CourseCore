using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Presentation.Presenters;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Access.Presentation.Controllers;

/// <summary>
/// Role and permission administration. Restricted to the Admin role on purpose:
/// it can not be delegated through a role permission.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthPolicyNames.AdminOnly)]
public class RoleManagementController : ControllerBase
{
    private readonly ListRoleDetailsUseCase _listRoleDetailsUseCase;
    private readonly ListPermissionsUseCase _listPermissionsUseCase;
    private readonly CreateRoleUseCase _createRoleUseCase;
    private readonly UpdateRoleUseCase _updateRoleUseCase;
    private readonly SetRolePermissionsUseCase _setRolePermissionsUseCase;

    public RoleManagementController(
        ListRoleDetailsUseCase listRoleDetailsUseCase,
        ListPermissionsUseCase listPermissionsUseCase,
        CreateRoleUseCase createRoleUseCase,
        UpdateRoleUseCase updateRoleUseCase,
        SetRolePermissionsUseCase setRolePermissionsUseCase)
    {
        _listRoleDetailsUseCase = listRoleDetailsUseCase;
        _listPermissionsUseCase = listPermissionsUseCase;
        _createRoleUseCase = createRoleUseCase;
        _updateRoleUseCase = updateRoleUseCase;
        _setRolePermissionsUseCase = setRolePermissionsUseCase;
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<RoleDetailResponse>>> ListRolesAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listRoleDetailsUseCase.ExecuteAsync(cancellationToken);

        return Ok(output.Select(RolePresenter.ToResponse).ToList());
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionResponse>>> ListPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listPermissionsUseCase.ExecuteAsync(cancellationToken);

        return Ok(output.Select(RolePresenter.ToResponse).ToList());
    }

    [HttpPost("roles")]
    [ProducesResponseType(typeof(RoleDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDetailResponse>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createRoleUseCase.ExecuteAsync(RolePresenter.ToInput(request), cancellationToken);
        var response = RolePresenter.ToResponse(output);

        return Created($"/api/admin/roles/{response.Id}", response);
    }

    [HttpPut("roles/{roleId:guid}")]
    [ProducesResponseType(typeof(RoleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDetailResponse>> UpdateRoleAsync(
        Guid roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateRoleUseCase.ExecuteAsync(RolePresenter.ToInput(roleId, request), cancellationToken);

        return Ok(RolePresenter.ToResponse(output));
    }

    [HttpPut("roles/{roleId:guid}/permissions")]
    [ProducesResponseType(typeof(RoleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDetailResponse>> SetRolePermissionsAsync(
        Guid roleId,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _setRolePermissionsUseCase.ExecuteAsync(
            RolePresenter.ToInput(roleId, request),
            cancellationToken);

        return Ok(RolePresenter.ToResponse(output));
    }
}
