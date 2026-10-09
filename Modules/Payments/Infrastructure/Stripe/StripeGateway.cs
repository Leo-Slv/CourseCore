using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Application.Options;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CourseCore.Api.Modules.Payments.Infrastructure.Stripe;

public sealed class StripeGateway : IStripeGateway
{
    private readonly StripeOptions _options;

    public StripeGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = string.IsNullOrWhiteSpace(_options.ApiBase)
            ? new StripeClient(_options.SecretKey)
            : new StripeClient(new StripeClientOptions
            {
                ApiKey = _options.SecretKey,
                ApiBase = _options.ApiBase.Trim()
            });
        var service = new SessionService(client);

        var session = await service.CreateAsync(
            new SessionCreateOptions
            {
                Mode = "payment",
                ClientReferenceId = request.OrderId.ToString(),
                CustomerEmail = request.CustomerEmail,
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = request.OrderId.ToString(),
                    ["userId"] = request.UserId.ToString(),
                    ["courseId"] = request.CourseId.ToString()
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency,
                            UnitAmount = request.AmountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = request.CourseTitle
                            }
                        }
                    }
                ]
            },
            requestOptions: new RequestOptions { IdempotencyKey = $"checkout-{request.OrderId}" },
            cancellationToken: cancellationToken);

        return new CheckoutSessionResult(session.Id, session.Url);
    }
}
