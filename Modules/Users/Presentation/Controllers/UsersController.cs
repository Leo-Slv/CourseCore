using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Modules.Users.Presentation.Presenters;
using CourseCore.Api.Modules.Users.Presentation.Requests;
using CourseCore.Api.Modules.Users.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Users.Presentation.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthPolicyNames.ManageUsers)]
public class UsersController : ControllerBase
{
    private readonly CreateUserUseCase _createUserUseCase;
    private readonly UpdateUserUseCase _updateUserUseCase;
    private readonly ListUsersUseCase _listUsersUseCase;
    private readonly GetUserByIdUseCase _getUserByIdUseCase;
    private readonly AssignUserRoleUseCase _assignUserRoleUseCase;
    private readonly RemoveUserRoleUseCase _removeUserRoleUseCase;

    public UsersController(
        CreateUserUseCase createUserUseCase,
        UpdateUserUseCase updateUserUseCase,
        ListUsersUseCase listUsersUseCase,
        GetUserByIdUseCase getUserByIdUseCase,
        AssignUserRoleUseCase assignUserRoleUseCase,
        RemoveUserRoleUseCase removeUserRoleUseCase)
    {
        _createUserUseCase = createUserUseCase;
        _updateUserUseCase = updateUserUseCase;
        _listUsersUseCase = listUsersUseCase;
        _getUserByIdUseCase = getUserByIdUseCase;
        _assignUserRoleUseCase = assignUserRoleUseCase;
        _removeUserRoleUseCase = removeUserRoleUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createUserUseCase.ExecuteAsync(UserPresenter.ToInput(request), cancellationToken);
        var response = UserPresenter.ToResponse(output);

        return Created($"/api/users/{response.Id}", response);
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateUserUseCase.ExecuteAsync(
            UserPresenter.ToInput(userId, request),
            cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserListResponse>> ListAsync(
        [FromQuery] ListUsersRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _listUsersUseCase.ExecuteAsync(UserPresenter.ToInput(request), cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var output = await _getUserByIdUseCase.ExecuteAsync(userId, cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }

    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        await _assignUserRoleUseCase.ExecuteAsync(userId, roleId, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        await _removeUserRoleUseCase.ExecuteAsync(userId, roleId, cancellationToken);

        return NoContent();
    }
}
