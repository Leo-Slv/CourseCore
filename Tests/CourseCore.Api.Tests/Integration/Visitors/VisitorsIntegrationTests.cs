using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Visitors.Presentation.Responses;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using CourseCore.Api.Shared.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourseCore.Api.Tests.Integration.Visitors;

public class VisitorsIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public VisitorsIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WhenAnonymousWithValidData_ShouldReturnCreatedWithoutPersonalData()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var email = UniqueEmail();
        var usersBefore = await CountUsersAsync();

        var response = await client.PostAsJsonAsync("/api/visitors", new
        {
            name = "Ana Lima",
            phone = "(11) 98765-4321",
            email,
            address = "Rua A, 10, Centro, São Paulo",
            captchaToken = "token"
        });
        var rawBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));

        using var document = JsonDocument.Parse(rawBody);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToList();
        Assert.Equal(["id", "submittedAt"], properties);
        Assert.DoesNotContain(email, rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(usersBefore, await CountUsersAsync());

        var id = document.RootElement.GetProperty("id").GetGuid();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var stored = await dbContext.Visitors.SingleAsync(visitor => visitor.Id == id);
        Assert.Equal("11987654321", stored.Phone);
        Assert.Equal(email.ToLowerInvariant(), stored.Email);

        var auditLog = await dbContext.AuditLogs.SingleAsync(log =>
            log.Action == AuditLogActionNames.VisitorRegistered && log.EntityId == id);
        Assert.Null(auditLog.UserId);
        Assert.DoesNotContain("Ana", auditLog.MetadataJson ?? string.Empty);
        Assert.DoesNotContain(email, auditLog.MetadataJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("98765", auditLog.MetadataJson ?? string.Empty);
    }

    [Fact]
    public async Task Register_WhenAddressIsOmitted_ShouldReturnCreated()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/visitors", new
        {
            name = "Bruno Costa",
            phone = "1134567890",
            email = UniqueEmail(),
            captchaToken = "token"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenSameDataIsSubmittedTwice_ShouldCreateTwoRecords()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var payload = new
        {
            name = "Carla Dias",
            phone = "(21) 99876-5432",
            email = UniqueEmail(),
            captchaToken = "token"
        };

        var first = await client.PostAsJsonAsync("/api/visitors", payload);
        var second = await client.PostAsJsonAsync("/api/visitors", payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<RegisterVisitorResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<RegisterVisitorResponse>();
        Assert.NotEqual(firstBody!.Id, secondBody!.Id);
    }

    [Theory]
    [InlineData("A", "(11) 98765-4321", "valid@example.com")]
    [InlineData("Ana Lima", "(11) 8765-432", "valid@example.com")]
    [InlineData("Ana Lima", "+55 (11) 98765-4321", "valid@example.com")]
    [InlineData("Ana Lima", "(11) 98765-4321", "not-an-email")]
    [InlineData("", "(11) 98765-4321", "valid@example.com")]
    public async Task Register_WhenDataIsInvalid_ShouldReturnBadRequest(string name, string phone, string email)
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/visitors", new
        {
            name,
            phone,
            email,
            captchaToken = "token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenCaptchaIsInvalid_ShouldReturnBadRequestAndPersistNothing()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var captcha = (FakeCaptchaVerificationService)_factory.Services.GetRequiredService<ICaptchaVerificationService>();
        var email = UniqueEmail();
        captcha.Result = false;

        try
        {
            var response = await client.PostAsJsonAsync("/api/visitors", new
            {
                name = "Ana Lima",
                phone = "(11) 98765-4321",
                email,
                captchaToken = "invalid"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            captcha.Result = true;
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        Assert.False(await dbContext.Visitors.AnyAsync(visitor => visitor.Email == email));
    }

    [Fact]
    public async Task List_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync("/api/visitors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WhenAuthenticatedWithoutPermission_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/visitors");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WhenUserHasReadVisitorsPermission_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync(AuthPermissionNames.ReadVisitors);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/visitors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_WhenAdmin_ShouldReturnNewestFirstAndSearchByNameEmailOrPhone()
    {
        using var anonymousClient = IntegrationAuth.CreateClient(_factory);
        var marker = Guid.NewGuid().ToString("N")[..8];
        var olderEmail = $"older.{marker}@example.com";
        var newerEmail = $"newer.{marker}@example.com";
        var newerPhone = $"119{Random.Shared.Next(10000000, 99999999)}";

        await PostVisitorAsync(anonymousClient, $"Older {marker}", "(31) 3456-7890", olderEmail);
        await PostVisitorAsync(anonymousClient, $"Newer {marker}", newerPhone, newerEmail);

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var byName = await ListAsync(adminClient, marker);
        Assert.Equal([newerEmail, olderEmail], byName.Items.Select(item => item.Email));

        var byEmail = await ListAsync(adminClient, newerEmail.ToUpperInvariant());
        Assert.Equal(newerEmail, Assert.Single(byEmail.Items).Email);

        var maskedPhone = $"({newerPhone[..2]}) {newerPhone[2..7]}-{newerPhone[7..]}";
        var byPhone = await ListAsync(adminClient, maskedPhone);
        var match = Assert.Single(byPhone.Items);
        Assert.Equal(newerPhone, match.Phone);
        Assert.Equal($"Newer {marker}", match.Name);
    }

    [Fact]
    public async Task List_WhenPageSizeIsInvalid_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/visitors?pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenRequestsExceedRateLimit_ShouldReturnTooManyRequests()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:VisitorRegistration:PermitLimit"] = "2",
            ["RateLimiting:VisitorRegistration:WindowSeconds"] = "60",
            ["RateLimiting:VisitorRegistration:QueueLimit"] = "0"
        });
        using var client = IntegrationAuth.CreateClient(factory);

        var first = await PostVisitorAsync(client, "Ana Lima", "11987654321", UniqueEmail());
        var second = await PostVisitorAsync(client, "Ana Lima", "11987654321", UniqueEmail());
        var third = await PostVisitorAsync(client, "Ana Lima", "11987654321", UniqueEmail());

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    private static string UniqueEmail()
    {
        return $"visitor.{Guid.NewGuid():N}@example.com";
    }

    private static Task<HttpResponseMessage> PostVisitorAsync(HttpClient client, string name, string phone, string email)
    {
        return client.PostAsJsonAsync("/api/visitors", new
        {
            name,
            phone,
            email,
            captchaToken = "token"
        });
    }

    private static async Task<PagedResponse<VisitorResponse>> ListAsync(HttpClient client, string search)
    {
        var response = await client.GetAsync($"/api/visitors?search={Uri.EscapeDataString(search)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<PagedResponse<VisitorResponse>>())!;
    }

    private async Task<int> CountUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

        return await dbContext.Users.CountAsync();
    }
}
