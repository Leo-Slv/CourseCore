using System.Text.Json;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.AuditLogs;

public class AuditLogServiceTests
{
    [Fact]
    public async Task RecordAsync_ShouldUseCurrentUserAndCorrelationId()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeAuditLogRepository();
        var currentUser = new FakeCurrentUserService { UserId = userId, IsAuthenticated = true };
        var requestContext = new FakeRequestContextService { CorrelationId = "correlation-123" };
        var service = new AuditLogService(repository, currentUser, requestContext);

        await service.RecordAsync(
            "SensitiveAction",
            "Entity",
            Guid.NewGuid(),
            new Dictionary<string, string?>
            {
                ["result"] = " ok ",
                ["blank"] = " ",
                ["empty"] = null
            });

        var auditLog = Assert.Single(repository.AuditLogs);
        Assert.Equal(userId, auditLog.UserId);
        Assert.NotNull(auditLog.MetadataJson);

        using var metadata = JsonDocument.Parse(auditLog.MetadataJson);
        Assert.Equal("ok", metadata.RootElement.GetProperty("result").GetString());
        Assert.Equal("correlation-123", metadata.RootElement.GetProperty("correlationId").GetString());
        Assert.False(metadata.RootElement.TryGetProperty("blank", out _));
        Assert.False(metadata.RootElement.TryGetProperty("empty", out _));
    }

    [Fact]
    public async Task RecordAsync_WhenExplicitUserIsProvided_ShouldPreferExplicitUser()
    {
        var currentUserId = Guid.NewGuid();
        var explicitUserId = Guid.NewGuid();
        var repository = new FakeAuditLogRepository();
        var currentUser = new FakeCurrentUserService { UserId = currentUserId, IsAuthenticated = true };
        var requestContext = new FakeRequestContextService();
        var service = new AuditLogService(repository, currentUser, requestContext);

        await service.RecordAsync("Action", "Entity", userId: explicitUserId);

        var auditLog = Assert.Single(repository.AuditLogs);
        Assert.Equal(explicitUserId, auditLog.UserId);
        Assert.Null(auditLog.MetadataJson);
    }
}
