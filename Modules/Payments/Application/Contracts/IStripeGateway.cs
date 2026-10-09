namespace CourseCore.Api.Modules.Payments.Application.Contracts;

public interface IStripeGateway
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CreateCheckoutSessionRequest(
    Guid OrderId,
    Guid UserId,
    Guid CourseId,
    string CourseTitle,
    long AmountInCents,
    string Currency,
    string CustomerEmail,
    string SuccessUrl,
    string CancelUrl);

public sealed record CheckoutSessionResult(string SessionId, string Url);
