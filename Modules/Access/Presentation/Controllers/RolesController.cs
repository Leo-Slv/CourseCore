using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Presentation.Presenters;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Access.Presentation.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = AuthPolicyNames.ManageUsers)]
public class RolesController : ControllerBase
{
    private readonly ListRolesUseCase _listRolesUseCase;

    public RolesController(ListRolesUseCase listRolesUseCase)
    {
        _listRolesUseCase = listRolesUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var output = await _listRolesUseCase.ExecuteAsync(cancellationToken);

        return Ok(output.Select(RolePresenter.ToResponse).ToList());
    }
}
