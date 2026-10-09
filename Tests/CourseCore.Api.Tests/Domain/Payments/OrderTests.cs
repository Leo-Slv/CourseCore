using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Enums;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Tests.Domain.Payments;

public class OrderTests
{
    [Fact]
    public void Create_ShouldStartPendingWithNormalizedCurrency()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 4990, "BRL");

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal("brl", order.Currency);
        Assert.Null(order.PaidAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenAmountIsNotPositive_ShouldThrow(long amount)
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.NewGuid(), Guid.NewGuid(), amount));
    }

    [Fact]
    public void Create_WhenIdsAreEmpty_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, Guid.NewGuid(), 100));
        Assert.Throws<DomainException>(() => Order.Create(Guid.NewGuid(), Guid.Empty, 100));
    }

    [Fact]
    public void MarkPaid_WhenPending_ShouldTransitionOnceAndBeIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 4990);
        var paidAt = DateTime.UtcNow;

        var first = order.MarkPaid(paidAt);
        var second = order.MarkPaid(paidAt.AddMinutes(5));

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(paidAt, order.PaidAt);
    }

    [Fact]
    public void MarkPaid_WhenExpired_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 4990);
        order.MarkExpired();

        Assert.Throws<DomainException>(() => order.MarkPaid(DateTime.UtcNow));
    }

    [Fact]
    public void MarkExpiredAndMarkFailed_WhenAlreadyPaid_ShouldKeepPaid()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 4990);
        order.MarkPaid(DateTime.UtcNow);

        order.MarkExpired();
        order.MarkFailed();

        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void AttachCheckoutSession_WhenNotPending_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), 4990);
        order.MarkFailed();

        Assert.Throws<DomainException>(() => order.AttachCheckoutSession("cs_test_1"));
    }
}
