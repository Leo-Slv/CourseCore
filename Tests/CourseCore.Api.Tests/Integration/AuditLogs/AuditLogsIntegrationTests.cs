using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Presentation.Responses;
using CourseCore.Api.Shared.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.AuditLogs;

public class AuditLogsIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public AuditLogsIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListAuditLogs_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/audit-logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListAuditLogs_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListAuditLogs_WhenUserHasReadAuditPermission_ShouldReturnOk()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserAsync(AuthPermissionNames.ReadAudit);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/audit-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListAuditLogs_AfterCourseIsCreated_ShouldIncludeCourseCreatedActionNewestFirst()
    {
        using var client = CreateClient();
        var areaId = await _factory.SeedAreaAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/courses", new
        {
            title = "Audited Course",
            slug = $"audited-course-{Guid.NewGuid():N}",
            description = "Audited course",
            displayOrder = 0,
            pricingModel = "Paid",
            areaIds = new[] { areaId },
            modules = Array.Empty<object>()
        });
        var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var courseId = created.GetProperty("id").GetGuid();

        var response = await client.GetAsync("/api/audit-logs");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AuditLogResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var entry = body!.Items.Single(item => item.EntityId == courseId);
        Assert.Equal("CourseCreated", entry.Action);
        Assert.Equal("Course", entry.EntityName);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
