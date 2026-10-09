namespace CourseCore.Api.Modules.Payments.Application.Contracts;

public interface IStripeWebhookVerifier
{
    /// <summary>
    /// Validates the Stripe-Signature header against the raw payload and returns the parsed event.
    /// Throws <see cref="CourseCore.Api.Shared.Application.Exceptions.ApplicationValidationException"/>
    /// when the signature or payload is invalid.
    /// </summary>
    StripeWebhookEvent Verify(string payload, string? signatureHeader);
}

public sealed record StripeWebhookEvent(
    string Id,
    string Type,
    string? SessionId,
    string? ClientReferenceId,
    string? PaymentStatus,
    long? AmountTotal,
    string? Currency);
