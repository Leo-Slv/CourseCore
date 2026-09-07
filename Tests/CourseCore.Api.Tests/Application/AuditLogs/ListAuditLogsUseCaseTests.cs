using CourseCore.Api.Modules.AuditLogs.Application.DTOs;
using CourseCore.Api.Modules.AuditLogs.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.AuditLogs;

public class ListAuditLogsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnMostRecentFirst()
    {
        var repository = new FakeAuditLogRepository();
        var older = AuditLog.Restore(
            Guid.NewGuid(), Guid.NewGuid(), "CourseCreated", "Course", Guid.NewGuid(), null,
            DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-10));
        var newer = AuditLog.Restore(
            Guid.NewGuid(), Guid.NewGuid(), "CoursePublished", "Course", Guid.NewGuid(), null,
            DateTime.UtcNow, DateTime.UtcNow);
        repository.AuditLogs.Add(older);
        repository.AuditLogs.Add(newer);
        var useCase = new ListAuditLogsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListAuditLogsInput());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(newer.Id, result.Items.First().Id);
        Assert.Equal(older.Id, result.Items.Last().Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldParseMetadataJson()
    {
        var repository = new FakeAuditLogRepository();
        var log = AuditLog.Restore(
            Guid.NewGuid(), Guid.NewGuid(), "UserAreaAccessGranted", "UserAreaAccess", Guid.NewGuid(),
            """{"areaId":"11111111-1111-1111-1111-111111111111","canView":"True"}""",
            DateTime.UtcNow, DateTime.UtcNow);
        repository.AuditLogs.Add(log);
        var useCase = new ListAuditLogsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListAuditLogsInput());

        var output = Assert.Single(result.Items);
        Assert.Equal("11111111-1111-1111-1111-111111111111", output.Metadata["areaId"]);
        Assert.Equal("True", output.Metadata["canView"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectPageSize()
    {
        var repository = new FakeAuditLogRepository();
        for (var i = 0; i < 5; i++)
        {
            repository.AuditLogs.Add(AuditLog.Restore(
                Guid.NewGuid(), Guid.NewGuid(), "CourseCreated", "Course", Guid.NewGuid(), null,
                DateTime.UtcNow.AddMinutes(-i), DateTime.UtcNow.AddMinutes(-i)));
        }
        var useCase = new ListAuditLogsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListAuditLogsInput { Page = 1, PageSize = 2 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageIsInvalid_ShouldThrow()
    {
        var repository = new FakeAuditLogRepository();
        var useCase = new ListAuditLogsUseCase(repository);

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new ListAuditLogsInput { Page = 0 }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageSizeExceedsMaximum_ShouldThrow()
    {
        var repository = new FakeAuditLogRepository();
        var useCase = new ListAuditLogsUseCase(repository);

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new ListAuditLogsInput { PageSize = 1000 }));
    }
}
