using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Presentation.Presenters;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Access.Presentation.Controllers;

[ApiController]
[Route("api/areas")]
[Authorize(Policy = AuthPolicyNames.ManageAreas)]
public class AreaManagementController : ControllerBase
{
    private readonly CreateAreaUseCase _createAreaUseCase;
    private readonly UpdateAreaUseCase _updateAreaUseCase;
    private readonly GetAreaByIdUseCase _getAreaByIdUseCase;
    private readonly ListAreasUseCase _listAreasUseCase;

    public AreaManagementController(
        CreateAreaUseCase createAreaUseCase,
        UpdateAreaUseCase updateAreaUseCase,
        GetAreaByIdUseCase getAreaByIdUseCase,
        ListAreasUseCase listAreasUseCase)
    {
        _createAreaUseCase = createAreaUseCase;
        _updateAreaUseCase = updateAreaUseCase;
        _getAreaByIdUseCase = getAreaByIdUseCase;
        _listAreasUseCase = listAreasUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AreaResponse>> CreateAsync(
        CreateAreaRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createAreaUseCase.ExecuteAsync(AreaPresenter.ToInput(request), cancellationToken);
        var response = AreaPresenter.ToResponse(output);

        return Created($"/api/areas/{response.Id}", response);
    }

    [HttpPut("{areaId:guid}")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AreaResponse>> UpdateAsync(
        Guid areaId,
        UpdateAreaRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateAreaUseCase.ExecuteAsync(
            AreaPresenter.ToInput(areaId, request),
            cancellationToken);

        return Ok(AreaPresenter.ToResponse(output));
    }

    [HttpGet("{areaId:guid}")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AreaResponse>> GetByIdAsync(
        Guid areaId,
        CancellationToken cancellationToken)
    {
        var output = await _getAreaByIdUseCase.ExecuteAsync(areaId, cancellationToken);

        return Ok(AreaPresenter.ToResponse(output));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AreaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<AreaResponse>>> ListAsync(
        [FromQuery] ListAreasRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _listAreasUseCase.ExecuteAsync(AreaPresenter.ToInput(request), cancellationToken);

        return Ok(AreaPresenter.ToResponse(output));
    }
}
