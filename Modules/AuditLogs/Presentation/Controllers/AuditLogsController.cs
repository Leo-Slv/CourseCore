using CourseCore.Api.Modules.AuditLogs.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Presentation.Presenters;
using CourseCore.Api.Modules.AuditLogs.Presentation.Requests;
using CourseCore.Api.Modules.AuditLogs.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.AuditLogs.Presentation.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthPolicyNames.ReadAudit)]
public class AuditLogsController : ControllerBase
{
    private readonly ListAuditLogsUseCase _listAuditLogsUseCase;

    public AuditLogsController(ListAuditLogsUseCase listAuditLogsUseCase)
    {
        _listAuditLogsUseCase = listAuditLogsUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> ListAsync(
        [FromQuery] ListAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _listAuditLogsUseCase.ExecuteAsync(AuditLogPresenter.ToInput(request), cancellationToken);

        return Ok(AuditLogPresenter.ToResponse(output));
    }
}
