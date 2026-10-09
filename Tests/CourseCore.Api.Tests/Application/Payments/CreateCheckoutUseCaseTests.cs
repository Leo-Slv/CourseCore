using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Payments.Application.UseCases;
using CourseCore.Api.Modules.Payments.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Payments;

public class CreateCheckoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPaidCourseWithPrice_ShouldCreatePendingOrderAndReturnCheckoutUrl()
    {
        var fixture = CreateFixture(price: 49.90m);

        var output = await fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id);

        var order = Assert.Single(fixture.Orders.Orders);
        Assert.Equal(order.Id, output.OrderId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(4990, order.AmountInCents);
        Assert.Equal("brl", order.Currency);
        Assert.Equal($"cs_test_{order.Id:N}", order.StripeCheckoutSessionId);
        Assert.Equal($"https://checkout.stripe.test/{order.Id:N}", output.CheckoutUrl);
        Assert.Single(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.OrderCreated);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldChargeThePriceStoredOnTheCourseAndBuildReturnUrls()
    {
        var fixture = CreateFixture(price: 199.99m);

        await fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id);

        var request = Assert.Single(fixture.Stripe.Requests);
        Assert.Equal(19999, request.AmountInCents);
        Assert.Equal(fixture.Course.Title, request.CourseTitle);
        Assert.Equal(fixture.UserEmail, request.CustomerEmail);
        Assert.Equal($"https://app.coursecore.local/checkout/success?orderId={request.OrderId}", request.SuccessUrl);
        Assert.Equal($"https://app.coursecore.local/checkout/cancel?orderId={request.OrderId}", request.CancelUrl);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseIsFree_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));

        Assert.Empty(fixture.Orders.Orders);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaidCourseHasNoPrice_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(price: null);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseIsNotPublished_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture();
        fixture.Course.Unpublish();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, Guid.NewGuid()));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserAlreadyHasAccess_ShouldThrowConflictException()
    {
        var fixture = CreateFixture();
        fixture.CourseAccesses.Accesses.Add(
            UserCourseAccess.Create(fixture.UserId, fixture.Course.Id, CourseAccessSource.Manual));

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));

        Assert.Empty(fixture.Stripe.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsNotVerified_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(emailVerified: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(userActive: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseHasNoActiveArea_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(areaActive: false);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));

        Assert.Empty(fixture.Orders.Orders);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStripeFails_ShouldMarkOrderFailedAndThrowExternalServiceException()
    {
        var fixture = CreateFixture();
        fixture.Stripe.ShouldFail = true;

        await Assert.ThrowsAsync<ExternalServiceException>(
            () => fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.Course.Id));

        var order = Assert.Single(fixture.Orders.Orders);
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Null(order.StripeCheckoutSessionId);
    }

    private static CheckoutFixture CreateFixture(
        decimal? price = 49.90m,
        CoursePricingModel pricingModel = CoursePricingModel.Paid,
        bool emailVerified = true,
        bool userActive = true,
        bool areaActive = true)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var courseAccesses = new FakeUserCourseAccessRepository();
        var orders = new FakeOrderRepository();
        var stripe = new FakeStripeGateway();
        var auditLogs = new FakeAuditLogService();
        var user = TestEntityFactory.User(email: $"buyer-{Guid.NewGuid():N}@coursecore.local", active: userActive, emailVerified: emailVerified);
        var area = TestEntityFactory.Area(active: areaActive);
        var course = TestEntityFactory.PublishedCourse(area.Id, pricingModel);

        if (price is not null && pricingModel == CoursePricingModel.Paid)
        {
            course.ChangePriceAmount(price);
        }

        users.Add(user);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        var useCase = new CreateCheckoutUseCase(
            users,
            courses,
            orders,
            new CourseAccessService(users, roles, areas, courses, courseAccesses),
            stripe,
            new FakeUnitOfWork(),
            auditLogs,
            Options.Create(new FrontendOptions { BaseUrl = "https://app.coursecore.local/" }));

        return new CheckoutFixture(useCase, user.Id, user.Email.Value, course, orders, stripe, courseAccesses, auditLogs);
    }

    private sealed record CheckoutFixture(
        CreateCheckoutUseCase UseCase,
        Guid UserId,
        string UserEmail,
        Course Course,
        FakeOrderRepository Orders,
        FakeStripeGateway Stripe,
        FakeUserCourseAccessRepository CourseAccesses,
        FakeAuditLogService AuditLogs);
}
