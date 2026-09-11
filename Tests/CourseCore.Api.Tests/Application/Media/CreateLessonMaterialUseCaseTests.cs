using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class CreateLessonMaterialUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenLessonExists_ShouldCreateMaterial()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new CreateLessonMaterialInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Apostila — Módulo 01",
            FileName = "modulo-01.pdf",
            ContentType = "application/pdf",
            StorageProvider = "Local",
            StorageKey = "materials/modulo-01.pdf",
            SizeBytes = 1_200_000
        });

        var material = Assert.Single(fixture.Materials.Materials);
        Assert.Equal(fixture.Lesson.Id, output.LessonId);
        Assert.Equal(0, material.DisplayOrder);
        var auditLog = Assert.Single(fixture.AuditLogs.Entries, e => e.Action == AuditLogActionNames.LessonMaterialCreated);
        Assert.Equal("Apostila — Módulo 01", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonAlreadyHasMaterials_ShouldAppendNextDisplayOrder()
    {
        var fixture = CreateFixture();
        await fixture.UseCase.ExecuteAsync(new CreateLessonMaterialInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Material 1",
            FileName = "material-1.pdf",
            ContentType = "application/pdf",
            StorageProvider = "Local",
            StorageKey = "materials/material-1.pdf",
            SizeBytes = 1024
        });

        var output = await fixture.UseCase.ExecuteAsync(new CreateLessonMaterialInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Material 2",
            FileName = "material-2.pdf",
            ContentType = "application/pdf",
            StorageProvider = "Local",
            StorageKey = "materials/material-2.pdf",
            SizeBytes = 2048
        });

        Assert.Equal(2, fixture.Materials.Materials.Count);
        Assert.Equal(1, output.DisplayOrder);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new CreateLessonMaterialInput
        {
            LessonId = Guid.NewGuid(),
            Title = "Material",
            FileName = "material.pdf",
            ContentType = "application/pdf",
            StorageProvider = "Local",
            StorageKey = "materials/material.pdf",
            SizeBytes = 1024
        }));
    }

    [Theory]
    [InlineData("https://media.example/material.pdf")]
    [InlineData("../material.pdf")]
    [InlineData("materials/../material.pdf")]
    [InlineData("/materials/material.pdf")]
    [InlineData("materials\\material.pdf")]
    public async Task ExecuteAsync_WhenStorageKeyIsInvalid_ShouldRejectIt(string storageKey)
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAnyAsync<Exception>(() => fixture.UseCase.ExecuteAsync(new CreateLessonMaterialInput
        {
            LessonId = fixture.Lesson.Id,
            Title = "Material",
            FileName = "material.pdf",
            ContentType = "application/pdf",
            StorageProvider = "Local",
            StorageKey = storageKey,
            SizeBytes = 1024
        }));
    }

    private static CreateLessonMaterialFixture CreateFixture()
    {
        var materials = new FakeLessonMaterialRepository();
        var lessons = new FakeLessonRepository();
        var auditLogs = new FakeAuditLogService();
        var lesson = Lesson.Create(Guid.NewGuid(), "Lesson", "Description", displayOrder: 0);
        lessons.Lessons.Add(lesson);
        var useCase = new CreateLessonMaterialUseCase(materials, lessons, new FakeUnitOfWork(), auditLogs);

        return new CreateLessonMaterialFixture(useCase, materials, auditLogs, lesson);
    }

    private sealed record CreateLessonMaterialFixture(
        CreateLessonMaterialUseCase UseCase,
        FakeLessonMaterialRepository Materials,
        FakeAuditLogService AuditLogs,
        Lesson Lesson);
}
