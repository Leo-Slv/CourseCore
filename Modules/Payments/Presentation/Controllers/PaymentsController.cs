using CourseCore.Api.Modules.Payments.Application.UseCases;
using CourseCore.Api.Modules.Payments.Presentation.Presenters;
using CourseCore.Api.Modules.Payments.Presentation.Responses;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Modules.Payments.Presentation.Controllers;

[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly CreateCheckoutUseCase _createCheckoutUseCase;
    private readonly ListMyOrdersUseCase _listMyOrdersUseCase;
    private readonly HandleStripeWebhookUseCase _handleStripeWebhookUseCase;
    private readonly ICurrentUserService _currentUserService;

    public PaymentsController(
        CreateCheckoutUseCase createCheckoutUseCase,
        ListMyOrdersUseCase listMyOrdersUseCase,
        HandleStripeWebhookUseCase handleStripeWebhookUseCase,
        ICurrentUserService currentUserService)
    {
        _createCheckoutUseCase = createCheckoutUseCase;
        _listMyOrdersUseCase = listMyOrdersUseCase;
        _handleStripeWebhookUseCase = handleStripeWebhookUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost("api/courses/{courseId:guid}/checkout")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CheckoutResponse>> CreateCheckoutAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var output = await _createCheckoutUseCase.ExecuteAsync(GetCurrentUserId(), courseId, cancellationToken);

        return Ok(PaymentPresenter.ToResponse(output));
    }

    [HttpGet("api/orders/me")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<OrderResponse>>> ListMyOrdersAsync(
        CancellationToken cancellationToken)
    {
        var outputs = await _listMyOrdersUseCase.ExecuteAsync(GetCurrentUserId(), cancellationToken);

        return Ok(outputs.Select(PaymentPresenter.ToResponse).ToList());
    }

    [HttpPost("api/payments/stripe/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StripeWebhookAsync(CancellationToken cancellationToken)
    {
        // The signature is computed over the exact raw body, so it must not be model-bound.
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        await _handleStripeWebhookUseCase.ExecuteAsync(
            payload,
            Request.Headers["Stripe-Signature"].FirstOrDefault(),
            cancellationToken);

        return Ok();
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
