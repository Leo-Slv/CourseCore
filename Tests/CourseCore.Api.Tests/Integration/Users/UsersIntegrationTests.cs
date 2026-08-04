using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Users.Application.Validation;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Users;

public class UsersIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public UsersIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WhenAdminIsAuthenticated_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutQuery_ShouldReturnDefaultPaginationMetadata()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var json = await client.GetFromJsonAsync<JsonElement>("/api/users");
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(50, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(JsonValueKind.Array, json.GetProperty("items").ValueKind);
        Assert.True(json.TryGetProperty("totalItems", out _));
        Assert.True(json.TryGetProperty("totalPages", out _));
    }

    [Fact]
    public async Task GetUsers_WithPageSizeTwo_ShouldReturnPaginationMetadata()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var json = await client.GetFromJsonAsync<JsonElement>("/api/users?page=1&pageSize=2");
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(2, json.GetProperty("pageSize").GetInt32());
        Assert.True(json.GetProperty("items").GetArrayLength() <= 2);
    }

    [Theory]
    [InlineData("page=0&pageSize=2")]
    [InlineData("page=1&pageSize=0")]
    [InlineData("page=1&pageSize=101")]
    public async Task GetUsers_WithInvalidPagination_ShouldReturnBadRequest(string query)
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var response = await client.GetAsync($"/api/users?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenAdminPostsValidRequest_ShouldReturnCreated()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        const string password = "IntegrationUser123!";

        var response = await client.PostAsJsonAsync("/api/users", CreateUserRequest(password));
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(json.RootElement.GetProperty("id").GetGuid() != Guid.Empty);
        Assert.DoesNotContain(password, content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateUser_WhenUserHasManageUsersPermission_ShouldReturnCreated()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/users", CreateUserRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/users", CreateUserRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/users", CreateUserRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenRequestIsInvalid_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = string.Empty,
            email = $"invalid-{Guid.NewGuid():N}@coursecore.local",
            password = "IntegrationUser123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenNameIsTooLong_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = new string('N', UserValidationLimits.NameMaxLength + 1),
            email = $"large-{Guid.NewGuid():N}@coursecore.local",
            password = "StrongIntegrationPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("            ")]
    [InlineData("password123")]
    public async Task CreateUser_WhenPasswordIsWeak_ShouldReturnBadRequest(string password)
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "Weak Password User",
            email = $"weak-{Guid.NewGuid():N}@coursecore.local",
            password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenEmailIsTooLong_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "Large Email User",
            email = $"{new string('e', UserValidationLimits.EmailMaxLength)}@example.com",
            password = "StrongIntegrationPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var user = await _factory.SeedUserAsync();

        var response = await client.PutAsJsonAsync($"/api/users/{user.Id}", new
        {
            name = "Updated Integration User",
            email = $"updated-{Guid.NewGuid():N}@coursecore.local",
            active = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new
        {
            name = "Missing Integration User",
            email = $"missing-{Guid.NewGuid():N}@coursecore.local",
            active = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static object CreateUserRequest(string password = "IntegrationUser123!")
    {
        return new
        {
            name = "Created Integration User",
            email = $"created-{Guid.NewGuid():N}@coursecore.local",
            password
        };
    }
}
