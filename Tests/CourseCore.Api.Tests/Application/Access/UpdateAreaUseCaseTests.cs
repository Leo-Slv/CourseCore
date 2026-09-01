using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class UpdateAreaUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAreaExists_ShouldUpdateAllFields()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateAreaUseCase(areas, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new UpdateAreaInput
        {
            AreaId = area.Id,
            Name = "Updated",
            Slug = "updated-area",
            Description = "Updated description",
            DisplayOrder = 5,
            Active = true
        });

        Assert.Equal("Updated", output.Name);
        Assert.Equal("updated-area", output.Slug);
        Assert.Equal("Updated description", output.Description);
        Assert.Equal(5, output.DisplayOrder);
        Assert.Contains(auditLogs.Entries, entry => entry.Action == AuditLogActionNames.AreaUpdated);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActiveChangesToFalse_ShouldDeactivateAndRecordAuditLog()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateAreaUseCase(areas, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(ValidInput(area.Id, area.Slug.Value, active: false));

        Assert.False(output.Active);
        Assert.Contains(auditLogs.Entries, entry => entry.Action == AuditLogActionNames.AreaDeactivated);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActiveDoesNotChange_ShouldNotRecordActivationAuditLog()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateAreaUseCase(areas, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(ValidInput(area.Id, area.Slug.Value, active: true));

        Assert.DoesNotContain(
            auditLogs.Entries,
            entry => entry.Action is AuditLogActionNames.AreaActivated or AuditLogActionNames.AreaDeactivated);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAreaDoesNotExist_ShouldThrowNotFound()
    {
        var useCase = new UpdateAreaUseCase(new FakeAreaRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => useCase.ExecuteAsync(ValidInput(Guid.NewGuid(), "missing-area")));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugConflictsWithAnotherArea_ShouldThrowConflict()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        var otherArea = TestEntityFactory.Area();
        areas.Areas.Add(area);
        areas.Areas.Add(otherArea);
        var useCase = new UpdateAreaUseCase(areas, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(
            () => useCase.ExecuteAsync(ValidInput(area.Id, otherArea.Slug.Value)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugIsUnchanged_ShouldNotThrowConflict()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var useCase = new UpdateAreaUseCase(areas, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(ValidInput(area.Id, area.Slug.Value));

        Assert.Equal(area.Slug.Value, output.Slug);
    }

    private static UpdateAreaInput ValidInput(Guid areaId, string slug, bool active = true)
    {
        return new UpdateAreaInput
        {
            AreaId = areaId,
            Name = "Area",
            Slug = slug,
            Description = "Description",
            DisplayOrder = 0,
            Active = active
        };
    }
}
