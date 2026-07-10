using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Modules.Users.Presentation.Presenters;
using CourseCore.Api.Modules.Users.Presentation.Requests;
using CourseCore.Api.Modules.Users.Presentation.Responses;
using CourseCore.Api.Shared.Domain.Exceptions;
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
        try
        {
            var output = await _createUserUseCase.ExecuteAsync(UserPresenter.ToInput(request), cancellationToken);

            return Ok(UserPresenter.ToResponse(output));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _updateUserUseCase.ExecuteAsync(
                UserPresenter.ToInput(userId, request),
                cancellationToken);

            return Ok(UserPresenter.ToResponse(output));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listUsersUseCase.ExecuteAsync(cancellationToken);

        return Ok(UserPresenter.ToResponse(output));
    }
}
