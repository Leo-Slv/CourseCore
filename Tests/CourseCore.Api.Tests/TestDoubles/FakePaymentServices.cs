using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> Orders { get; } = [];

    public Task<Order?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Orders.FirstOrDefault(order => order.Id == id));
    }

    public Task<Order?> FindByCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Orders.FirstOrDefault(order => order.StripeCheckoutSessionId == sessionId));
    }

    public Task<IReadOnlyCollection<Order>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Order>>(
            Orders.Where(order => order.UserId == userId).OrderByDescending(order => order.CreatedAt).ToArray());
    }

    public Task CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        Orders.Add(order);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class FakeStripeGateway : IStripeGateway
{
    public List<CreateCheckoutSessionRequest> Requests { get; } = [];

    public bool ShouldFail { get; set; }

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("Stripe is down.");
        }

        Requests.Add(request);

        return Task.FromResult(new CheckoutSessionResult(
            $"cs_test_{request.OrderId:N}",
            $"https://checkout.stripe.test/{request.OrderId:N}"));
    }
}

public sealed class FakeStripeWebhookVerifier : IStripeWebhookVerifier
{
    public StripeWebhookEvent? NextEvent { get; set; }

    public bool SignatureIsValid { get; set; } = true;

    public StripeWebhookEvent Verify(string payload, string? signatureHeader)
    {
        if (!SignatureIsValid || NextEvent is null)
        {
            throw new ApplicationValidationException("Stripe signature is invalid.");
        }

        return NextEvent;
    }
}
