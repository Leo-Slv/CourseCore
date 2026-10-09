using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Enums;
using CourseCore.Api.Modules.Payments.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace CourseCore.Api.Modules.Payments.Application.UseCases;

public class HandleStripeWebhookUseCase
{
    private const string CheckoutSessionCompleted = "checkout.session.completed";
    private const string CheckoutSessionAsyncPaymentSucceeded = "checkout.session.async_payment_succeeded";
    private const string CheckoutSessionExpired = "checkout.session.expired";
    private const string PaidStatus = "paid";

    private readonly IStripeWebhookVerifier _verifier;
    private readonly IOrderRepository _orders;
    private readonly UserCourseAccessGranter _courseAccessGranter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;
    private readonly ILogger<HandleStripeWebhookUseCase> _logger;

    public HandleStripeWebhookUseCase(
        IStripeWebhookVerifier verifier,
        IOrderRepository orders,
        UserCourseAccessGranter courseAccessGranter,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs,
        ILogger<HandleStripeWebhookUseCase> logger)
    {
        _verifier = verifier;
        _orders = orders;
        _courseAccessGranter = courseAccessGranter;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        string payload,
        string? signatureHeader,
        CancellationToken cancellationToken = default)
    {
        var stripeEvent = _verifier.Verify(payload, signatureHeader);

        switch (stripeEvent.Type)
        {
            case CheckoutSessionCompleted:
            case CheckoutSessionAsyncPaymentSucceeded:
                await HandlePaidAsync(stripeEvent, cancellationToken);
                break;
            case CheckoutSessionExpired:
                await HandleExpiredAsync(stripeEvent, cancellationToken);
                break;
            default:
                _logger.LogDebug("Ignoring Stripe event {EventId} of type {EventType}.", stripeEvent.Id, stripeEvent.Type);
                break;
        }
    }

    private async Task HandlePaidAsync(StripeWebhookEvent stripeEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(stripeEvent.PaymentStatus, PaidStatus, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Stripe event {EventId} is not paid yet (payment_status={PaymentStatus}); waiting for a later event.",
                stripeEvent.Id,
                stripeEvent.PaymentStatus);
            return;
        }

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var order = await FindOrderAsync(stripeEvent, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Stripe event {EventId} does not match any order.", stripeEvent.Id);
                return;
            }

            if (order.Status == OrderStatus.Paid)
            {
                return;
            }

            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogError(
                    "Stripe event {EventId} reports payment for order {OrderId} in status {Status}; manual review required.",
                    stripeEvent.Id,
                    order.Id,
                    order.Status);
                return;
            }

            if (!MatchesOrder(order, stripeEvent))
            {
                _logger.LogError(
                    "Stripe event {EventId} does not match order {OrderId} (session, amount or currency); ignoring.",
                    stripeEvent.Id,
                    order.Id);
                return;
            }

            order.MarkPaid(DateTime.UtcNow);
            await _orders.UpdateAsync(order, cancellationToken);
            await _courseAccessGranter.EnsureGrantedAsync(
                order.UserId,
                order.CourseId,
                CourseAccessSource.Purchase,
                cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.OrderPaid,
                "Order",
                order.Id,
                new Dictionary<string, string?>
                {
                    ["courseId"] = order.CourseId.ToString(),
                    ["amountInCents"] = order.AmountInCents.ToString(),
                    ["stripeEventId"] = stripeEvent.Id,
                    ["displayName"] = order.Id.ToString()
                },
                userId: order.UserId,
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }

    private async Task HandleExpiredAsync(StripeWebhookEvent stripeEvent, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var order = await FindOrderAsync(stripeEvent, cancellationToken);

            if (order is null || order.Status != OrderStatus.Pending)
            {
                return;
            }

            order.MarkExpired();
            await _orders.UpdateAsync(order, cancellationToken);
        }, cancellationToken);
    }

    private async Task<Order?> FindOrderAsync(StripeWebhookEvent stripeEvent, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(stripeEvent.ClientReferenceId, out var orderId))
        {
            return await _orders.FindByIdAsync(orderId, cancellationToken);
        }

        return string.IsNullOrWhiteSpace(stripeEvent.SessionId)
            ? null
            : await _orders.FindByCheckoutSessionIdAsync(stripeEvent.SessionId, cancellationToken);
    }

    private static bool MatchesOrder(Order order, StripeWebhookEvent stripeEvent)
    {
        return string.Equals(order.StripeCheckoutSessionId, stripeEvent.SessionId, StringComparison.Ordinal)
            && order.AmountInCents == stripeEvent.AmountTotal
            && string.Equals(order.Currency, stripeEvent.Currency, StringComparison.OrdinalIgnoreCase);
    }
}
