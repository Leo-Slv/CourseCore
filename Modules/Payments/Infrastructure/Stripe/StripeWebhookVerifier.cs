using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Application.Options;
using CourseCore.Api.Shared.Application.Exceptions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CourseCore.Api.Modules.Payments.Infrastructure.Stripe;

public sealed class StripeWebhookVerifier : IStripeWebhookVerifier
{
    private readonly StripeOptions _options;

    public StripeWebhookVerifier(IOptions<StripeOptions> options)
    {
        _options = options.Value;
    }

    public StripeWebhookEvent Verify(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new ApplicationValidationException("Stripe signature is missing.");
        }

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                _options.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            throw new ApplicationValidationException("Stripe signature is invalid.");
        }

        if (stripeEvent.Data.Object is Session session)
        {
            return new StripeWebhookEvent(
                stripeEvent.Id,
                stripeEvent.Type,
                session.Id,
                session.ClientReferenceId,
                session.PaymentStatus,
                session.AmountTotal,
                session.Currency);
        }

        return new StripeWebhookEvent(stripeEvent.Id, stripeEvent.Type, null, null, null, null, null);
    }
}
