using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Presentation.Presenters;
using CourseCore.Api.Modules.Courses.Presentation.Requests;
using CourseCore.Api.Modules.Courses.Presentation.Responses;
using CourseCore.Api.Shared.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Courses.Presentation.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly CreateCourseUseCase _createCourseUseCase;
    private readonly UpdateCourseUseCase _updateCourseUseCase;
    private readonly PublishCourseUseCase _publishCourseUseCase;
    private readonly GetCourseDetailsUseCase _getCourseDetailsUseCase;
    private readonly ListAvailableCoursesUseCase _listAvailableCoursesUseCase;

    public CoursesController(
        CreateCourseUseCase createCourseUseCase,
        UpdateCourseUseCase updateCourseUseCase,
        PublishCourseUseCase publishCourseUseCase,
        GetCourseDetailsUseCase getCourseDetailsUseCase,
        ListAvailableCoursesUseCase listAvailableCoursesUseCase)
    {
        _createCourseUseCase = createCourseUseCase;
        _updateCourseUseCase = updateCourseUseCase;
        _publishCourseUseCase = publishCourseUseCase;
        _getCourseDetailsUseCase = getCourseDetailsUseCase;
        _listAvailableCoursesUseCase = listAvailableCoursesUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<CourseResponse>> CreateAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _createCourseUseCase.ExecuteAsync(
                CoursePresenter.ToInput(request),
                cancellationToken);

            return Ok(CoursePresenter.ToResponse(output));
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

    [HttpPut("{courseId:guid}")]
    public async Task<ActionResult<CourseResponse>> UpdateAsync(
        Guid courseId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _updateCourseUseCase.ExecuteAsync(
                CoursePresenter.ToInput(courseId, request),
                cancellationToken);

            return Ok(CoursePresenter.ToResponse(output));
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

    [HttpPost("{courseId:guid}/publish")]
    public async Task<ActionResult<CourseResponse>> PublishAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _publishCourseUseCase.ExecuteAsync(
                new PublishCourseInput { CourseId = courseId },
                cancellationToken);

            return Ok(CoursePresenter.ToResponse(output));
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

    [HttpGet("{courseId:guid}")]
    public async Task<ActionResult<CourseDetailsResponse>> GetDetailsAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _getCourseDetailsUseCase.ExecuteAsync(
                new GetCourseDetailsInput { CourseId = courseId },
                cancellationToken);

            return Ok(CoursePresenter.ToResponse(output));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyCollection<CourseListItemResponse>>> ListAvailableAsync(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await _listAvailableCoursesUseCase.ExecuteAsync(
                new ListAvailableCoursesInput { UserId = userId },
                cancellationToken);

            return Ok(CoursePresenter.ToResponse(output));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
