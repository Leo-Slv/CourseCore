using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Payments.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Payments;

public class PaymentsIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public PaymentsIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Checkout_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsync($"/api/courses/{Guid.NewGuid()}/checkout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WhenPaidCourse_ShouldReturnCheckoutUrlAndPendingOrder()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(priceAmount: 49.90m);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsync($"/api/courses/{course.CourseId}/checkout", content: null);
        var body = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.StartsWith("https://checkout.stripe.test/", body.CheckoutUrl);
        var order = Assert.Single(orders!);
        Assert.Equal(body.OrderId, order.Id);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(4990, order.AmountInCents);
    }

    [Fact]
    public async Task Checkout_WhenCourseHasNoPrice_ShouldReturnConflict()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsync($"/api/courses/{course.CourseId}/checkout", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WhenUserAlreadyHasAccess_ShouldReturnConflict()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id, priceAmount: 49.90m);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsync($"/api/courses/{course.CourseId}/checkout", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenSignatureIsInvalid_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var payload = BuildEvent("evt_bad", "checkout.session.completed", "cs_test_x", Guid.NewGuid(), 100, "paid");

        var response = await PostWebhookAsync(client, payload, "t=1,v1=deadbeef");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenSignatureIsMissing_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var payload = BuildEvent("evt_bad", "checkout.session.completed", "cs_test_x", Guid.NewGuid(), 100, "paid");

        var response = await PostWebhookAsync(client, payload, signature: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseFlow_WhenSignedPaidWebhookArrives_ShouldGrantAccessAndStayIdempotent()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(priceAmount: 49.90m);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var beforeAccess = await GetAccessAsync(client, course.CourseId);
        var checkout = await (await client.PostAsync($"/api/courses/{course.CourseId}/checkout", content: null))
            .Content.ReadFromJsonAsync<CheckoutResponse>();
        var payload = BuildEvent(
            "evt_paid",
            "checkout.session.completed",
            $"cs_test_{checkout!.OrderId:N}",
            checkout.OrderId,
            4990,
            "paid");

        var first = await PostWebhookAsync(client, payload, Sign(payload));
        var second = await PostWebhookAsync(client, payload, Sign(payload));
        var afterAccess = await GetAccessAsync(client, course.CourseId);
        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders/me");

        Assert.False(beforeAccess.CanAccess);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(afterAccess.CanAccess);
        var order = Assert.Single(orders!);
        Assert.Equal("Paid", order.Status);
        Assert.NotNull(order.PaidAt);

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);
        var grants = await adminClient.GetFromJsonAsync<List<UserCourseAccessResponse>>(
            $"/api/access/user-course/{user.Id}");
        var grant = Assert.Single(grants!);
        Assert.Equal("Purchase", grant.Source);
    }

    [Fact]
    public async Task PurchaseFlow_WhenWebhookReportsUnpaidSession_ShouldNotGrantAccess()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(priceAmount: 49.90m);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);
        var checkout = await (await client.PostAsync($"/api/courses/{course.CourseId}/checkout", content: null))
            .Content.ReadFromJsonAsync<CheckoutResponse>();
        var payload = BuildEvent(
            "evt_unpaid",
            "checkout.session.completed",
            $"cs_test_{checkout!.OrderId:N}",
            checkout.OrderId,
            4990,
            "unpaid");

        var response = await PostWebhookAsync(client, payload, Sign(payload));
        var access = await GetAccessAsync(client, course.CourseId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(access.CanAccess);
    }

    [Fact]
    public async Task PurchaseFlow_WhenSessionExpires_ShouldMarkOrderExpired()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(priceAmount: 49.90m);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);
        var checkout = await (await client.PostAsync($"/api/courses/{course.CourseId}/checkout", content: null))
            .Content.ReadFromJsonAsync<CheckoutResponse>();
        var payload = BuildEvent(
            "evt_expired",
            "checkout.session.expired",
            $"cs_test_{checkout!.OrderId:N}",
            checkout.OrderId,
            4990,
            "unpaid");

        await PostWebhookAsync(client, payload, Sign(payload));
        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders/me");

        Assert.Equal("Expired", Assert.Single(orders!).Status);
    }

    [Fact]
    public async Task ListMyOrders_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync("/api/orders/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<CourseAccessResponse> GetAccessAsync(HttpClient client, Guid courseId)
    {
        var body = await client.GetFromJsonAsync<CourseAccessResponse>($"/api/access/courses/{courseId}");

        return body!;
    }

    private static Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, string payload, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (signature is not null)
        {
            request.Headers.Add("Stripe-Signature", signature);
        }

        return client.SendAsync(request);
    }

    private static string BuildEvent(
        string eventId,
        string type,
        string sessionId,
        Guid orderId,
        long amountTotal,
        string paymentStatus)
    {
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return "{"
            + $"\"id\":\"{eventId}\","
            + "\"object\":\"event\","
            + "\"api_version\":\"2025-01-27.acacia\","
            + $"\"created\":{created},"
            + $"\"type\":\"{type}\","
            + "\"livemode\":false,"
            + "\"data\":{\"object\":{"
            + $"\"id\":\"{sessionId}\","
            + "\"object\":\"checkout.session\","
            + $"\"client_reference_id\":\"{orderId}\","
            + $"\"payment_status\":\"{paymentStatus}\","
            + $"\"amount_total\":{amountTotal},"
            + "\"currency\":\"brl\""
            + "}}}";
    }

    private static string Sign(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(CourseCoreApiFactory.StripeWebhookSecret),
            Encoding.UTF8.GetBytes(signedPayload));

        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
