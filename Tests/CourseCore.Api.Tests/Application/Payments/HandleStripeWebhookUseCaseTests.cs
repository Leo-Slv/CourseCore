using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Application.UseCases;
using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace CourseCore.Api.Tests.Application.Payments;

public class HandleStripeWebhookUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCheckoutIsPaid_ShouldMarkOrderPaidAndGrantCourseAccess()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent();

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
        Assert.NotNull(fixture.Order.PaidAt);
        var access = Assert.Single(fixture.CourseAccesses.Accesses);
        Assert.Equal(fixture.Order.UserId, access.UserId);
        Assert.Equal(fixture.Order.CourseId, access.CourseId);
        Assert.Equal(CourseAccessSource.Purchase, access.Source);
        var audit = Assert.Single(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.OrderPaid);
        Assert.Equal(fixture.Order.Id, audit.EntityId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSameEventIsDeliveredTwice_ShouldNotDuplicateGrantOrAudit()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent();

        await fixture.UseCase.ExecuteAsync("{}", "sig");
        var paidAt = fixture.Order.PaidAt;
        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Single(fixture.CourseAccesses.Accesses);
        Assert.Single(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.OrderPaid);
        Assert.Equal(paidAt, fixture.Order.PaidAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserAlreadyHasAccess_ShouldStillMarkOrderPaidWithoutDuplicatingGrant()
    {
        var fixture = CreateFixture();
        fixture.CourseAccesses.Accesses.Add(
            CourseCore.Api.Modules.Access.Domain.Entities.UserCourseAccess.Create(
                fixture.Order.UserId, fixture.Order.CourseId, CourseAccessSource.Manual));
        fixture.Verifier.NextEvent = fixture.PaidEvent();

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
        Assert.Single(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAsyncPaymentSucceeds_ShouldGrantAccess()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent(type: "checkout.session.async_payment_succeeded");

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
        Assert.Single(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCheckoutCompletedButNotPaid_ShouldNotGrantAccess()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent(paymentStatus: "unpaid");

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSignatureIsInvalid_ShouldThrowAndChangeNothing()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent();
        fixture.Verifier.SignatureIsValid = false;

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync("{}", "bad"));

        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("currency")]
    [InlineData("session")]
    public async Task ExecuteAsync_WhenEventDoesNotMatchOrder_ShouldNotGrantAccess(string mismatch)
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = mismatch switch
        {
            "amount" => fixture.PaidEvent(amountTotal: 1),
            "currency" => fixture.PaidEvent(currency: "usd"),
            _ => fixture.PaidEvent(sessionId: "cs_test_other")
        };

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderIsUnknown_ShouldIgnoreEvent()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent(clientReferenceId: Guid.NewGuid().ToString());

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionExpires_ShouldMarkPendingOrderExpired()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent(type: "checkout.session.expired", paymentStatus: "unpaid");

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Expired, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionExpiresAfterPayment_ShouldKeepOrderPaid()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = fixture.PaidEvent();
        await fixture.UseCase.ExecuteAsync("{}", "sig");
        fixture.Verifier.NextEvent = fixture.PaidEvent(type: "checkout.session.expired");

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Paid, fixture.Order.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaymentArrivesForExpiredOrder_ShouldNotGrantAccess()
    {
        var fixture = CreateFixture();
        fixture.Order.MarkExpired();
        fixture.Verifier.NextEvent = fixture.PaidEvent();

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Expired, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEventTypeIsUnrelated_ShouldDoNothing()
    {
        var fixture = CreateFixture();
        fixture.Verifier.NextEvent = new StripeWebhookEvent("evt_1", "customer.created", null, null, null, null, null);

        await fixture.UseCase.ExecuteAsync("{}", "sig");

        Assert.Equal(OrderStatus.Pending, fixture.Order.Status);
        Assert.Empty(fixture.CourseAccesses.Accesses);
    }

    private static WebhookFixture CreateFixture()
    {
        var orders = new FakeOrderRepository();
        var courseAccesses = new FakeUserCourseAccessRepository();
        var verifier = new FakeStripeWebhookVerifier();
        var auditLogs = new FakeAuditLogService();
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 4990);
        order.AttachCheckoutSession("cs_test_session");
        orders.Orders.Add(order);

        var useCase = new HandleStripeWebhookUseCase(
            verifier,
            orders,
            new UserCourseAccessGranter(courseAccesses),
            new FakeUnitOfWork(),
            auditLogs,
            NullLogger<HandleStripeWebhookUseCase>.Instance);

        return new WebhookFixture(useCase, order, courseAccesses, verifier, auditLogs);
    }

    private sealed record WebhookFixture(
        HandleStripeWebhookUseCase UseCase,
        Order Order,
        FakeUserCourseAccessRepository CourseAccesses,
        FakeStripeWebhookVerifier Verifier,
        FakeAuditLogService AuditLogs)
    {
        public StripeWebhookEvent PaidEvent(
            string type = "checkout.session.completed",
            string paymentStatus = "paid",
            long amountTotal = 4990,
            string currency = "brl",
            string sessionId = "cs_test_session",
            string? clientReferenceId = null)
        {
            return new StripeWebhookEvent(
                $"evt_{Guid.NewGuid():N}",
                type,
                sessionId,
                clientReferenceId ?? Order.Id.ToString(),
                paymentStatus,
                amountTotal,
                currency);
        }
    }
}
