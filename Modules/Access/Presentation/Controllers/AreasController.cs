using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Presentation.Presenters;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Access.Presentation.Controllers;

[ApiController]
[Route("api/access")]
[Authorize(Policy = AuthPolicyNames.ManageAccess)]
public class AreasController : ControllerBase
{
    private readonly GrantUserAreaAccessUseCase _grantUserAreaAccessUseCase;
    private readonly GrantRoleAreaAccessUseCase _grantRoleAreaAccessUseCase;
    private readonly CheckCourseAccessUseCase _checkCourseAccessUseCase;

    public AreasController(
        GrantUserAreaAccessUseCase grantUserAreaAccessUseCase,
        GrantRoleAreaAccessUseCase grantRoleAreaAccessUseCase,
        CheckCourseAccessUseCase checkCourseAccessUseCase)
    {
        _grantUserAreaAccessUseCase = grantUserAreaAccessUseCase;
        _grantRoleAreaAccessUseCase = grantRoleAreaAccessUseCase;
        _checkCourseAccessUseCase = checkCourseAccessUseCase;
    }

    [HttpPost("user-area")]
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
    public async Task<ActionResult<CourseAccessResponse>> CheckCourseAccessAsync(
        CheckCourseAccessRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _checkCourseAccessUseCase.ExecuteAsync(
            AccessPresenter.ToInput(request),
            cancellationToken);

        return Ok(AccessPresenter.ToResponse(output));
    }
}
