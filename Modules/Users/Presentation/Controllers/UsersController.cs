using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Modules.Users.Presentation.Presenters;
using CourseCore.Api.Modules.Users.Presentation.Requests;
using CourseCore.Api.Modules.Users.Presentation.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Users.Presentation.Controllers;

[ApiController]
[Route("api/users")]
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
    public async Task<ActionResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createUserUseCase.ExecuteAsync(UserPresenter.ToInput(request), cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }

    [HttpPut("{userId:guid}")]
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
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listUsersUseCase.ExecuteAsync(cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }
}
