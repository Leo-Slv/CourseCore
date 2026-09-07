using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Testimonials.Application.UseCases;
using CourseCore.Api.Modules.Testimonials.Presentation.Presenters;
using CourseCore.Api.Modules.Testimonials.Presentation.Requests;
using CourseCore.Api.Modules.Testimonials.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Testimonials.Presentation.Controllers;

[ApiController]
[Route("api/testimonials")]
[Authorize(Policy = AuthPolicyNames.ManageCourses)]
public class TestimonialsController : ControllerBase
{
    private readonly CreateTestimonialUseCase _createTestimonialUseCase;
    private readonly UpdateTestimonialUseCase _updateTestimonialUseCase;
    private readonly PublishTestimonialUseCase _publishTestimonialUseCase;
    private readonly UnpublishTestimonialUseCase _unpublishTestimonialUseCase;
    private readonly ListTestimonialsUseCase _listTestimonialsUseCase;
    private readonly ListPublicTestimonialsUseCase _listPublicTestimonialsUseCase;

    public TestimonialsController(
        CreateTestimonialUseCase createTestimonialUseCase,
        UpdateTestimonialUseCase updateTestimonialUseCase,
        PublishTestimonialUseCase publishTestimonialUseCase,
        UnpublishTestimonialUseCase unpublishTestimonialUseCase,
        ListTestimonialsUseCase listTestimonialsUseCase,
        ListPublicTestimonialsUseCase listPublicTestimonialsUseCase)
    {
        _createTestimonialUseCase = createTestimonialUseCase;
        _updateTestimonialUseCase = updateTestimonialUseCase;
        _publishTestimonialUseCase = publishTestimonialUseCase;
        _unpublishTestimonialUseCase = unpublishTestimonialUseCase;
        _listTestimonialsUseCase = listTestimonialsUseCase;
        _listPublicTestimonialsUseCase = listPublicTestimonialsUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TestimonialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestimonialResponse>> CreateAsync(
        CreateTestimonialRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _createTestimonialUseCase.ExecuteAsync(
            TestimonialPresenter.ToInput(request),
            cancellationToken);
        var response = TestimonialPresenter.ToResponse(output);

        return Created($"/api/testimonials/{response.Id}", response);
    }

    [HttpPut("{testimonialId:guid}")]
    [ProducesResponseType(typeof(TestimonialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestimonialResponse>> UpdateAsync(
        Guid testimonialId,
        UpdateTestimonialRequest request,
        CancellationToken cancellationToken)
    {
        var output = await _updateTestimonialUseCase.ExecuteAsync(
            TestimonialPresenter.ToInput(testimonialId, request),
            cancellationToken);

        return Ok(TestimonialPresenter.ToResponse(output));
    }

    [HttpPost("{testimonialId:guid}/publish")]
    [ProducesResponseType(typeof(TestimonialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestimonialResponse>> PublishAsync(
        Guid testimonialId,
        CancellationToken cancellationToken)
    {
        var output = await _publishTestimonialUseCase.ExecuteAsync(testimonialId, cancellationToken);

        return Ok(TestimonialPresenter.ToResponse(output));
    }

    [HttpPost("{testimonialId:guid}/unpublish")]
    [ProducesResponseType(typeof(TestimonialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestimonialResponse>> UnpublishAsync(
        Guid testimonialId,
        CancellationToken cancellationToken)
    {
        var output = await _unpublishTestimonialUseCase.ExecuteAsync(testimonialId, cancellationToken);

        return Ok(TestimonialPresenter.ToResponse(output));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TestimonialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<TestimonialResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var outputs = await _listTestimonialsUseCase.ExecuteAsync(cancellationToken);

        return Ok(outputs.Select(TestimonialPresenter.ToResponse).ToList());
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<TestimonialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<TestimonialResponse>>> ListPublicAsync(
        CancellationToken cancellationToken)
    {
        var outputs = await _listPublicTestimonialsUseCase.ExecuteAsync(cancellationToken);

        return Ok(outputs.Select(TestimonialPresenter.ToResponse).ToList());
    }
}
