using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class GrantCourseAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCourseIsFree_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(CoursePricingModel.Free);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(
            fixture.UserId, fixture.CourseId, fixture.AdminUserId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserAlreadyHasAccess_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(CoursePricingModel.Paid);
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(
            fixture.UserId, fixture.CourseId, fixture.AdminUserId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoExistingRequest_ShouldCreateApprovedRequestAndGrantAreaAccess()
    {
        var fixture = CreateFixture(CoursePricingModel.Paid);

        var output = await fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.CourseId, fixture.AdminUserId);

        Assert.Equal("Approved", output.Status);
        Assert.Single(fixture.AccessRequests.Requests);
        var access = fixture.Areas.UserAreaAccesses.Single(a => a.UserId == fixture.UserId && a.AreaId == fixture.AreaId);
        Assert.True(access.CanView);
        var auditLog = Assert.Single(fixture.AuditLogs.Entries, e => e.Action == AuditLogActionNames.AccessRequestApproved);
        Assert.Equal($"{fixture.UserEmail} → {fixture.CourseTitle}", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPendingRequestExists_ShouldApproveExistingRequestInstead()
    {
        var fixture = CreateFixture(CoursePricingModel.Paid);
        var existingRequest = AccessRequest.Create(fixture.UserId, fixture.CourseId);
        fixture.AccessRequests.Requests.Add(existingRequest);

        var output = await fixture.UseCase.ExecuteAsync(fixture.UserId, fixture.CourseId, fixture.AdminUserId);

        Assert.Equal(existingRequest.Id, output.Id);
        Assert.Equal("Approved", output.Status);
        Assert.Single(fixture.AccessRequests.Requests);
    }

    private static GrantCourseAccessFixture CreateFixture(CoursePricingModel pricingModel)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var accessRequests = new FakeAccessRequestRepository();
        var user = TestEntityFactory.User(email: $"user-{Guid.NewGuid():N}@coursecore.local");
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id, pricingModel);

        users.Add(user);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var auditLogs = new FakeAuditLogService();
        var useCase = new GrantCourseAccessUseCase(
            users, courses, areas, accessRequests, courseAccessService, new FakeUnitOfWork(), auditLogs);

        return new GrantCourseAccessFixture(
            useCase, user.Id, course.Id, area.Id, Guid.NewGuid(), areas, accessRequests, auditLogs, user.Email.Value, course.Title);
    }

    private sealed record GrantCourseAccessFixture(
        GrantCourseAccessUseCase UseCase,
        Guid UserId,
        Guid CourseId,
        Guid AreaId,
        Guid AdminUserId,
        FakeAreaRepository Areas,
        FakeAccessRequestRepository AccessRequests,
        FakeAuditLogService AuditLogs,
        string UserEmail,
        string CourseTitle);
}
