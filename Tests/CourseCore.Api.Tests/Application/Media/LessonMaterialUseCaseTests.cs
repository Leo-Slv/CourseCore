using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
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
        var useCase = new ListLessonMaterialsUseCase(materials);

        var output = await useCase.ExecuteAsync(lessonId);

        Assert.Equal(["First", "Second"], output.Select(material => material.Title));
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
}
