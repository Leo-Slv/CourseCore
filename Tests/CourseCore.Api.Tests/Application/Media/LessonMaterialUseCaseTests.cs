using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class LessonMaterialUseCaseTests
{
    [Fact]
    public async Task ListLessonMaterialsUseCase_ShouldReturnMaterialsOrderedByDisplayOrder()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessonId = Guid.NewGuid();
        var second = LessonMaterial.Create(lessonId, "Second", "second.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/second.pdf", 100, 1);
        var first = LessonMaterial.Create(lessonId, "First", "first.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/first.pdf", 100, 0);
        materials.Materials.Add(second);
        materials.Materials.Add(first);
        var fixture = CreateListFixture(materials, lessonId, grantAccess: true);

        var output = await fixture.ListUseCase.ExecuteAsync(fixture.UserId, lessonId);

        Assert.Equal(["First", "Second"], output.Select(material => material.Title));
    }

    [Fact]
    public async Task ListLessonMaterialsUseCase_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessonId = Guid.NewGuid();
        var fixture = CreateListFixture(materials, lessonId, grantAccess: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.ListUseCase.ExecuteAsync(fixture.UserId, lessonId));
    }

    [Fact]
    public async Task ListLessonMaterialsUseCase_WhenBypassAccessCheckIsTrue_ShouldReturnMaterialsEvenWithoutAccess()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessonId = Guid.NewGuid();
        var fixture = CreateListFixture(materials, lessonId, grantAccess: false);

        var output = await fixture.ListUseCase.ExecuteAsync(fixture.UserId, lessonId, bypassAccessCheck: true);

        Assert.Empty(output);
    }

    [Fact]
    public async Task UpdateLessonMaterialUseCase_WhenMaterialExists_ShouldUpdateTitleAndDisplayOrder()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessonId = Guid.NewGuid();
        var material = LessonMaterial.Create(lessonId, "Old title", "material.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/material.pdf", 100, 0);
        materials.Materials.Add(material);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateLessonMaterialUseCase(materials, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new UpdateLessonMaterialInput
        {
            MaterialId = material.Id,
            Title = "New title",
            DisplayOrder = 3
        });

        Assert.Equal("New title", output.Title);
        Assert.Equal(3, output.DisplayOrder);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.LessonMaterialUpdated);
        Assert.Equal("New title", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task UpdateLessonMaterialUseCase_WhenMaterialDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new UpdateLessonMaterialUseCase(
            new FakeLessonMaterialRepository(),
            new FakeUnitOfWork(),
            new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new UpdateLessonMaterialInput
        {
            MaterialId = Guid.NewGuid(),
            Title = "Title",
            DisplayOrder = 0
        }));
    }

    [Fact]
    public async Task RemoveLessonMaterialUseCase_WhenMaterialExists_ShouldRemoveMaterial()
    {
        var materials = new FakeLessonMaterialRepository();
        var material = LessonMaterial.Create(Guid.NewGuid(), "Material", "material.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/material.pdf", 100, 0);
        materials.Materials.Add(material);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RemoveLessonMaterialUseCase(materials, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(material.Id);

        Assert.Empty(materials.Materials);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.LessonMaterialRemoved);
        Assert.Equal("Material", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task RemoveLessonMaterialUseCase_WhenMaterialDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new RemoveLessonMaterialUseCase(
            new FakeLessonMaterialRepository(),
            new FakeUnitOfWork(),
            new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReorderLessonMaterialsUseCase_WhenItemsMatchExistingMaterials_ShouldPersistNewOrder()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessonId = Guid.NewGuid();
        var first = LessonMaterial.Create(lessonId, "First", "first.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/first.pdf", 100, 0);
        var second = LessonMaterial.Create(lessonId, "Second", "second.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/second.pdf", 100, 1);
        materials.Materials.Add(first);
        materials.Materials.Add(second);
        var auditLogs = new FakeAuditLogService();
        var useCase = new ReorderLessonMaterialsUseCase(materials, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new ReorderLessonMaterialsInput
        {
            LessonId = lessonId,
            Items =
            [
                new ReorderLessonMaterialItem { MaterialId = first.Id, DisplayOrder = 1 },
                new ReorderLessonMaterialItem { MaterialId = second.Id, DisplayOrder = 0 }
            ]
        });

        Assert.Equal(1, first.DisplayOrder);
        Assert.Equal(0, second.DisplayOrder);
        Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.LessonMaterialsReordered);
    }

    [Fact]
    public async Task ReorderLessonMaterialsUseCase_WhenItemsDoNotMatchExistingMaterials_ShouldThrowApplicationValidationException()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessonId = Guid.NewGuid();
        var material = LessonMaterial.Create(lessonId, "Material", "material.pdf", "application/pdf", MaterialStorageProvider.Local, "materials/material.pdf", 100, 0);
        materials.Materials.Add(material);
        var useCase = new ReorderLessonMaterialsUseCase(materials, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new ReorderLessonMaterialsInput
        {
            LessonId = lessonId,
            Items =
            [
                new ReorderLessonMaterialItem { MaterialId = Guid.NewGuid(), DisplayOrder = 0 }
            ]
        }));
    }

    private static ListMaterialsFixture CreateListFixture(
        FakeLessonMaterialRepository materials,
        Guid lessonId,
        bool grantAccess)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var lessons = new FakeLessonRepository();
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var course = Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", displayOrder: 0);
        var module = CourseModule.Create(course.Id, "Module", "Description", displayOrder: 0);
        var lesson = Lesson.Restore(
            lessonId,
            module.Id,
            "Lesson",
            "Description",
            displayOrder: 0,
            freePreview: false,
            published: true,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow);
        module.AddLesson(lesson);
        course.AddModule(module);
        course.AttachArea(area.Id);
        course.Publish();

        users.Add(user);
        areas.Areas.Add(area);
        lessons.Lessons.Add(lesson);
        courses.Courses.Add(course);

        if (grantAccess)
        {
            areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: false));
        }

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var listUseCase = new ListLessonMaterialsUseCase(materials, lessons, courses, courseAccessService);

        return new ListMaterialsFixture(listUseCase, user.Id);
    }

    private sealed record ListMaterialsFixture(ListLessonMaterialsUseCase ListUseCase, Guid UserId);
}
