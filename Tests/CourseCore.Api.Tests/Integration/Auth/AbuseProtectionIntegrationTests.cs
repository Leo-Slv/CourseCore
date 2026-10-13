using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Auth;

/// <summary>
/// Protections against abuse: per-account login lockout and per-user / global rate limits.
/// </summary>
public class AbuseProtectionIntegrationTests
{
    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        return await client.PostAsJsonAsync("/api/auth/login", new { email, password });
    }

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_ShouldLockTheAccountEvenForTheCorrectPassword()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            // The per-IP limit is not what is being tested here.
            ["RateLimiting:Login:PermitLimit"] = "1000"
        });
        var user = await factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(factory);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await LoginAsync(client, user.Email, "WrongPassword!123");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        var locked = await LoginAsync(client, user.Email, user.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.True(locked.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.Parse(retryAfter!.First()) > 0);
    }

    [Fact]
    public async Task Login_ChangingTheIpHeader_ShouldNotBypassTheAccountLockout()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1000"
        });
        var user = await factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(factory);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { email = user.Email, password = "WrongPassword!123" })
            };
            request.Headers.Add("X-Forwarded-For", $"203.0.113.{attempt + 1}");
            await client.SendAsync(request);
        }

        using var correct = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = user.Email, password = user.Password })
        };
        correct.Headers.Add("X-Forwarded-For", "198.51.100.77");
        var response = await client.SendAsync(correct);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Login_LockingOneAccount_ShouldNotAffectAnotherUser()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1000"
        });
        var victim = await factory.SeedUserAsync();
        var other = await factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(factory);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(client, victim.Email, "WrongPassword!123");
        }

        var otherLogin = await LoginAsync(client, other.Email, other.Password);

        Assert.Equal(HttpStatusCode.OK, otherLogin.StatusCode);
    }

    [Fact]
    public async Task Login_ASuccessBeforeTheLimit_ShouldResetTheFailureCount()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = "1000"
        });
        var user = await factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(factory);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await LoginAsync(client, user.Email, "WrongPassword!123");
        }

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user.Email, user.Password)).StatusCode);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await LoginAsync(client, user.Email, "WrongPassword!123");
        }

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user.Email, user.Password)).StatusCode);
    }

    [Fact]
    public async Task Checkout_WhenAUserExceedsTheLimit_ShouldReturnTooManyRequestsOnlyForThatUser()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Checkout:PermitLimit"] = "2"
        });
        var spammer = await factory.SeedUserAsync();
        var bystander = await factory.SeedUserAsync();
        var courseId = Guid.NewGuid();

        using var spammerClient = IntegrationAuth.CreateClient(factory);
        await IntegrationAuth.AuthenticateAsAsync(spammerClient, spammer);

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var response = await spammerClient.PostAsync($"/api/courses/{courseId}/checkout", content: null);
            statuses.Add(response.StatusCode);
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(2));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[2]);

        // Same IP (the test host), different user: has its own budget.
        using var bystanderClient = IntegrationAuth.CreateClient(factory);
        await IntegrationAuth.AuthenticateAsAsync(bystanderClient, bystander);
        var bystanderResponse = await bystanderClient.PostAsync($"/api/courses/{courseId}/checkout", content: null);

        Assert.NotEqual(HttpStatusCode.TooManyRequests, bystanderResponse.StatusCode);
    }

    [Fact]
    public async Task UploadUrl_WhenAUserExceedsTheLimit_ShouldReturnTooManyRequests()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Upload:PermitLimit"] = "1"
        });
        using var client = IntegrationAuth.CreateClient(factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var first = await client.PostAsJsonAsync("/api/auth/me/avatar-upload-url", new { });
        var second = await client.PostAsJsonAsync("/api/auth/me/avatar-upload-url", new { });

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task GlobalLimit_WhenExceeded_ShouldReturnTooManyRequestsButNeverForHealthProbes()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Global:PermitLimit"] = "3"
        });
        using var client = IntegrationAuth.CreateClient(factory);

        var statuses = new List<HttpStatusCode>();
        for (var request = 0; request < 5; request++)
        {
            statuses.Add((await client.GetAsync("/api/courses/public-summary")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[4]);
        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(3));

        for (var probe = 0; probe < 10; probe++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        }
    }
}
