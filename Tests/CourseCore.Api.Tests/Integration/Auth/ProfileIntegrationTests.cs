using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Auth.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Auth;

public class ProfileIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public ProfileIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateProfile_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PutAsJsonAsync("/api/auth/me", new { name = "New Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WhenAuthenticated_ShouldUpdateNamePhoneAndAvatarUrl()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PutAsJsonAsync("/api/auth/me", new
        {
            name = "Updated Name",
            phone = "+55 11 99999-0000",
            avatarUrl = "https://cdn.coursecore.local/avatar.png"
        });
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Updated Name", body!.Name);
        Assert.Equal("+55 11 99999-0000", body.Phone);
        Assert.Equal("https://cdn.coursecore.local/avatar.png", body.AvatarUrl);

        var me = await client.GetAsync("/api/auth/me");
        var meBody = await me.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal("Updated Name", meBody!.Name);
    }

    [Fact]
    public async Task UpdateProfile_ShouldNotAcceptEmailChange()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PutAsJsonAsync("/api/auth/me", new
        {
            name = "Updated Name",
            email = "someone-else@coursecore.local"
        });
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.Email, body!.Email);
    }

    [Fact]
    public async Task ChangePassword_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "whatever",
            newPassword = "New_password_456!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordIsWrong_ShouldReturnUnauthorized()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Wrong_password_000!",
            newPassword = "New_password_456!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordIsWeak_ShouldReturnBadRequest()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = user.Password,
            newPassword = "weak"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_FullRoundTrip_ShouldChangePasswordAndInvalidateOldSessions()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = user.Password,
            newPassword = "New_password_789!"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var oldLogin = await IntegrationAuth.LoginAsync(client, user.Email, user.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await IntegrationAuth.LoginAsync(client, user.Email, "New_password_789!");
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WhenRequestsExceedRateLimit_ShouldReturnTooManyRequests()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:ChangePassword:PermitLimit"] = "2",
            ["RateLimiting:ChangePassword:WindowSeconds"] = "60",
            ["RateLimiting:ChangePassword:QueueLimit"] = "0"
        });
        var user = await factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var first = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Wrong_password_000!",
            newPassword = "New_password_456!"
        });
        var second = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Wrong_password_000!",
            newPassword = "New_password_456!"
        });
        var third = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Wrong_password_000!",
            newPassword = "New_password_456!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
