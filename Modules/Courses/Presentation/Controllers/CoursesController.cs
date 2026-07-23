using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Presentation.Presenters;
using CourseCore.Api.Modules.Courses.Presentation.Requests;
using CourseCore.Api.Modules.Courses.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Courses.Presentation.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly CreateCourseUseCase _createCourseUseCase;
    private readonly UpdateCourseUseCase _updateCourseUseCase;
    private readonly PublishCourseUseCase _publishCourseUseCase;
    private readonly GetCourseDetailsUseCase _getCourseDetailsUseCase;
    private readonly ListAvailableCoursesUseCase _listAvailableCoursesUseCase;
    private readonly ICurrentUserService _currentUserService;

    public CoursesController(
        CreateCourseUseCase createCourseUseCase,
        UpdateCourseUseCase updateCourseUseCase,
        PublishCourseUseCase publishCourseUseCase,
        GetCourseDetailsUseCase getCourseDetailsUseCase,
        ListAvailableCoursesUseCase listAvailableCoursesUseCase,
        ICurrentUserService currentUserService)
    {
        _createCourseUseCase = createCourseUseCase;
        _updateCourseUseCase = updateCourseUseCase;
        _publishCourseUseCase = publishCourseUseCase;
        _getCourseDetailsUseCase = getCourseDetailsUseCase;
        _listAvailableCoursesUseCase = listAvailableCoursesUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicyNames.ManageCourses)]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseResponse>> CreateAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createCourseUseCase.ExecuteAsync(
            CoursePresenter.ToInput(request),
            cancellationToken);
        var response = CoursePresenter.ToResponse(output);

        return CreatedAtAction(
            nameof(GetDetailsAsync),
            new { courseId = response.Id },
            response);
    }

    [HttpPut("{courseId:guid}")]
    [Authorize(Policy = AuthPolicyNames.ManageCourses)]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseResponse>> UpdateAsync(
        Guid courseId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateCourseUseCase.ExecuteAsync(
            CoursePresenter.ToInput(courseId, request),
            cancellationToken);

        return Ok(CoursePresenter.ToResponse(output));
    }

    [HttpPost("{courseId:guid}/publish")]
    [Authorize(Policy = AuthPolicyNames.ManageCourses)]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseResponse>> PublishAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var output = await _publishCourseUseCase.ExecuteAsync(
            new PublishCourseInput { CourseId = courseId },
            cancellationToken);

        return Ok(CoursePresenter.ToResponse(output));
    }

    [HttpGet("{courseId:guid}")]
    [ProducesResponseType(typeof(CourseDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseDetailsResponse>> GetDetailsAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var output = await _getCourseDetailsUseCase.ExecuteAsync(
            new GetCourseDetailsInput { CourseId = courseId },
            cancellationToken);

        return Ok(CoursePresenter.ToResponse(output));
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CourseListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<CourseListItemResponse>>> ListAvailableAsync(
        CancellationToken cancellationToken)
    {
        var output = await _listAvailableCoursesUseCase.ExecuteAsync(
            new ListAvailableCoursesInput { UserId = GetCurrentUserId() },
            cancellationToken);

        return Ok(CoursePresenter.ToResponse(output));
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
