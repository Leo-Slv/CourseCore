using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Presentation.Presenters;
using CourseCore.Api.Modules.Media.Presentation.Requests;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Media.Presentation.Controllers;

[ApiController]
[Route("api/materials")]
[Authorize]
public class LessonMaterialsController : ControllerBase
{
    private readonly CreateLessonMaterialUseCase _createLessonMaterialUseCase;
    private readonly ListLessonMaterialsUseCase _listLessonMaterialsUseCase;
    private readonly UpdateLessonMaterialUseCase _updateLessonMaterialUseCase;
    private readonly RemoveLessonMaterialUseCase _removeLessonMaterialUseCase;
    private readonly ReorderLessonMaterialsUseCase _reorderLessonMaterialsUseCase;
    private readonly RequestLessonMaterialUploadUseCase _requestLessonMaterialUploadUseCase;
    private readonly GetLessonMaterialDownloadUrlUseCase _getLessonMaterialDownloadUrlUseCase;
    private readonly ICurrentUserService _currentUserService;

    public LessonMaterialsController(
        CreateLessonMaterialUseCase createLessonMaterialUseCase,
        ListLessonMaterialsUseCase listLessonMaterialsUseCase,
        UpdateLessonMaterialUseCase updateLessonMaterialUseCase,
        RemoveLessonMaterialUseCase removeLessonMaterialUseCase,
        ReorderLessonMaterialsUseCase reorderLessonMaterialsUseCase,
        RequestLessonMaterialUploadUseCase requestLessonMaterialUploadUseCase,
        GetLessonMaterialDownloadUrlUseCase getLessonMaterialDownloadUrlUseCase,
        ICurrentUserService currentUserService)
    {
        _createLessonMaterialUseCase = createLessonMaterialUseCase;
        _listLessonMaterialsUseCase = listLessonMaterialsUseCase;
        _updateLessonMaterialUseCase = updateLessonMaterialUseCase;
        _removeLessonMaterialUseCase = removeLessonMaterialUseCase;
        _reorderLessonMaterialsUseCase = reorderLessonMaterialsUseCase;
        _requestLessonMaterialUploadUseCase = requestLessonMaterialUploadUseCase;
        _getLessonMaterialDownloadUrlUseCase = getLessonMaterialDownloadUrlUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost("upload-url")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(UploadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadUrlResponse>> RequestUploadUrlAsync(
        RequestUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _requestLessonMaterialUploadUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToUploadInput(request),
            cancellationToken);

        return Ok(LessonMaterialPresenter.ToResponse(output));
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LessonMaterialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<LessonMaterialResponse>>> ListLessonMaterialsAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var bypassAccessCheck = User.HasClaim(AuthClaimTypes.Permission, AuthPermissionNames.ManageVideos);
        var output = await _listLessonMaterialsUseCase.ExecuteAsync(
            GetCurrentUserId(),
            lessonId,
            bypassAccessCheck,
            cancellationToken);

        return Ok(LessonMaterialPresenter.ToResponse(output));
    }

    [HttpGet("{materialId:guid}/download")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(MaterialDownloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MaterialDownloadResponse>> GetDownloadUrlAsync(
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var bypassAccessCheck = User.HasClaim(AuthClaimTypes.Permission, AuthPermissionNames.ManageVideos);
        var output = await _getLessonMaterialDownloadUrlUseCase.ExecuteAsync(
            GetCurrentUserId(),
            materialId,
            bypassAccessCheck,
            cancellationToken);

        return Ok(LessonMaterialPresenter.ToResponse(output));
    }

    [HttpPost("lessons/{lessonId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(LessonMaterialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonMaterialResponse>> CreateLessonMaterialAsync(
        Guid lessonId,
        CreateLessonMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createLessonMaterialUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToInput(lessonId, request),
            cancellationToken);
        var response = LessonMaterialPresenter.ToResponse(output);

        return Created($"/api/materials/{response.Id}", response);
    }

    [HttpPut("{materialId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(typeof(LessonMaterialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LessonMaterialResponse>> UpdateLessonMaterialAsync(
        Guid materialId,
        UpdateLessonMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateLessonMaterialUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToInput(materialId, request),
            cancellationToken);

        return Ok(LessonMaterialPresenter.ToResponse(output));
    }

    [HttpDelete("{materialId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveLessonMaterialAsync(
        Guid materialId,
        CancellationToken cancellationToken)
    {
        await _removeLessonMaterialUseCase.ExecuteAsync(materialId, cancellationToken);

        return NoContent();
    }

    [HttpPatch("lessons/{lessonId:guid}/order")]
    [Authorize(Policy = AuthPolicyNames.ManageVideos)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReorderLessonMaterialsAsync(
        Guid lessonId,
        ReorderLessonMaterialsRequest request,
        CancellationToken cancellationToken)
    {
        await _reorderLessonMaterialsUseCase.ExecuteAsync(
            LessonMaterialPresenter.ToInput(lessonId, request),
            cancellationToken);

        return NoContent();
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
