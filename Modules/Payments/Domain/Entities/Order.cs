using CourseCore.Api.Modules.Payments.Domain.Enums;
using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Payments.Domain.Entities;

public class Order : EntityBase
{
    public const string BrlCurrency = "brl";

    private Order(Guid userId, Guid courseId, long amountInCents, string currency)
    {
        UserId = ValidateId(userId, nameof(UserId));
        CourseId = ValidateId(courseId, nameof(CourseId));
        AmountInCents = ValidateAmount(amountInCents);
        Currency = ValidateCurrency(currency);
        Status = OrderStatus.Pending;
    }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public long AmountInCents { get; private set; }

    public string Currency { get; private set; }

    public OrderStatus Status { get; private set; }

    public string? StripeCheckoutSessionId { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public static Order Create(Guid userId, Guid courseId, long amountInCents, string currency = BrlCurrency)
    {
        return new Order(userId, courseId, amountInCents, currency);
    }

    public static Order Restore(
        Guid id,
        Guid userId,
        Guid courseId,
        long amountInCents,
        string currency,
        OrderStatus status,
        string? stripeCheckoutSessionId,
        DateTime? paidAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Order(userId, courseId, amountInCents, currency)
        {
            Id = id,
            Status = status,
            StripeCheckoutSessionId = stripeCheckoutSessionId,
            PaidAt = paidAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void AttachCheckoutSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new DomainException("Checkout session id is required.");
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can receive a checkout session.");
        }

        StripeCheckoutSessionId = sessionId.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Idempotent. Returns true when the order transitioned to Paid, false when it was already Paid.
    /// </summary>
    public bool MarkPaid(DateTime paidAt)
    {
        if (Status == OrderStatus.Paid)
        {
            return false;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can be paid.");
        }

        Status = OrderStatus.Paid;
        PaidAt = paidAt;
        MarkAsUpdated();

        return true;
    }

    public void MarkExpired()
    {
        if (Status != OrderStatus.Pending)
        {
            return;
        }

        Status = OrderStatus.Expired;
        MarkAsUpdated();
    }

    public void MarkFailed()
    {
        if (Status != OrderStatus.Pending)
        {
            return;
        }

        Status = OrderStatus.Failed;
        MarkAsUpdated();
    }

    private static Guid ValidateId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return id;
    }

    private static long ValidateAmount(long amountInCents)
    {
        if (amountInCents <= 0)
        {
            throw new DomainException("Order amount must be greater than zero.");
        }

        return amountInCents;
    }

    private static string ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter ISO code.");
        }

        return currency.Trim().ToLowerInvariant();
    }
}
