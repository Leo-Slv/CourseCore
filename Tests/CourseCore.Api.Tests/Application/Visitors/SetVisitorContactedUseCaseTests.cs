using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Application.UseCases;
using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Visitors;

public class SetVisitorContactedUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenMarkingContacted_ShouldPersistAndAudit()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, true, fixture.AdminId);

        Assert.True(output.Contacted);
        Assert.NotNull(output.ContactedAt);
        Assert.Equal(fixture.AdminId, fixture.Visitor.ContactedByUserId);
        Assert.Equal(1, fixture.Visitors.UpdateCalls);
        var audit = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.VisitorMarkedContacted, audit.Action);
        Assert.Equal(fixture.Visitor.Id, audit.EntityId);
        Assert.Equal(fixture.AdminId, audit.UserId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyContacted_ShouldKeepOriginalDateAndNotAuditAgain()
    {
        var fixture = CreateFixture();
        await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, true, fixture.AdminId);
        var firstContactedAt = fixture.Visitor.ContactedAt;

        await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, true, Guid.NewGuid());

        Assert.Equal(firstContactedAt, fixture.Visitor.ContactedAt);
        Assert.Equal(fixture.AdminId, fixture.Visitor.ContactedByUserId);
        Assert.Equal(1, fixture.Visitors.UpdateCalls);
        Assert.Single(fixture.AuditLogs.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnmarking_ShouldClearContactAndAudit()
    {
        var fixture = CreateFixture();
        await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, true, fixture.AdminId);

        var output = await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, false, fixture.AdminId);

        Assert.False(output.Contacted);
        Assert.Null(output.ContactedAt);
        Assert.Null(fixture.Visitor.ContactedByUserId);
        Assert.Equal(AuditLogActionNames.VisitorMarkedNotContacted, fixture.AuditLogs.Entries.Last().Action);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnmarkingAVisitorNeverContacted_ShouldDoNothing()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, false, fixture.AdminId);

        Assert.False(output.Contacted);
        Assert.Equal(0, fixture.Visitors.UpdateCalls);
        Assert.Empty(fixture.AuditLogs.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVisitorDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(
            () => fixture.UseCase.ExecuteAsync(Guid.NewGuid(), true, fixture.AdminId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenIdsAreEmpty_ShouldThrow()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.UseCase.ExecuteAsync(Guid.Empty, true, fixture.AdminId));
        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, true, Guid.Empty));
    }

    [Fact]
    public async Task ListVisitors_ShouldFilterByContactedState()
    {
        var fixture = CreateFixture();
        var other = Visitor.Create("Outro Visitante", "11911112222", Email.Create("outro@example.com"), null);
        fixture.Visitors.Visitors.Add(other);
        await fixture.UseCase.ExecuteAsync(fixture.Visitor.Id, true, fixture.AdminId);
        var list = new ListVisitorsUseCase(fixture.Visitors);

        var pending = await list.ExecuteAsync(new ListVisitorsInput { Contacted = false });
        var done = await list.ExecuteAsync(new ListVisitorsInput { Contacted = true });
        var all = await list.ExecuteAsync(new ListVisitorsInput());

        Assert.Equal(other.Id, Assert.Single(pending.Items).Id);
        Assert.Equal(fixture.Visitor.Id, Assert.Single(done.Items).Id);
        Assert.Equal(2, all.TotalItems);
    }

    private static Fixture CreateFixture()
    {
        var visitors = new FakeVisitorRepository();
        var auditLogs = new FakeAuditLogService();
        var visitor = Visitor.Create("Ana Lima", "11987654321", Email.Create("ana@example.com"), null);
        visitors.Visitors.Add(visitor);

        return new Fixture(
            new SetVisitorContactedUseCase(visitors, new FakeUnitOfWork(), auditLogs),
            visitors,
            auditLogs,
            visitor,
            Guid.NewGuid());
    }

    private sealed record Fixture(
        SetVisitorContactedUseCase UseCase,
        FakeVisitorRepository Visitors,
        FakeAuditLogService AuditLogs,
        Visitor Visitor,
        Guid AdminId);
}
