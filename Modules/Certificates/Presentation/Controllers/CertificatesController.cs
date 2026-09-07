using CourseCore.Api.Modules.Certificates.Application.UseCases;
using CourseCore.Api.Modules.Certificates.Presentation.Presenters;
using CourseCore.Api.Modules.Certificates.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Certificates.Presentation.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly ListMyCertificatesUseCase _listMyCertificatesUseCase;
    private readonly ICurrentUserService _currentUserService;

    public CertificatesController(
        ListMyCertificatesUseCase listMyCertificatesUseCase,
        ICurrentUserService currentUserService)
    {
        _listMyCertificatesUseCase = listMyCertificatesUseCase;
        _currentUserService = currentUserService;
    }

    [HttpGet("mine")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(IReadOnlyCollection<CertificateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<CertificateResponse>>> ListMineAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listMyCertificatesUseCase.ExecuteAsync(GetCurrentUserId(), cancellationToken);

        return Ok(output.Select(CertificatePresenter.ToResponse).ToList());
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
