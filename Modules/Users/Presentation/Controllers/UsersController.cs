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

    public UsersController(
        CreateUserUseCase createUserUseCase,
        UpdateUserUseCase updateUserUseCase,
        ListUsersUseCase listUsersUseCase)
    {
        _createUserUseCase = createUserUseCase;
        _updateUserUseCase = updateUserUseCase;
        _listUsersUseCase = listUsersUseCase;
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
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listUsersUseCase.ExecuteAsync(cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }
}
