using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Application.Validation;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class CreateAreaUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDataIsValid_ShouldCreateActiveAreaAndRecordAuditLog()
    {
        var areas = new FakeAreaRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();
        var useCase = new CreateAreaUseCase(areas, unitOfWork, auditLogs);

        var output = await useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = "Courses",
            Slug = "courses",
            Description = "Access to course content",
            DisplayOrder = 10
        });

        Assert.True(output.Active);
        Assert.Equal("courses", output.Slug);
        Assert.Single(areas.Areas);
        Assert.Equal(1, unitOfWork.ExecuteCalls);
        Assert.Contains(auditLogs.Entries, entry => entry.Action == AuditLogActionNames.AreaCreated);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameIsEmpty_ShouldThrow()
    {
        var useCase = new CreateAreaUseCase(new FakeAreaRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = string.Empty,
            Slug = "courses",
            DisplayOrder = 0
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameIsTooLong_ShouldThrow()
    {
        var useCase = new CreateAreaUseCase(new FakeAreaRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = new string('n', AreaValidationLimits.NameMaxLength + 1),
            Slug = "courses",
            DisplayOrder = 0
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugIsInvalid_ShouldThrow()
    {
        var useCase = new CreateAreaUseCase(new FakeAreaRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = "Courses",
            Slug = "Not A Slug",
            DisplayOrder = 0
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisplayOrderIsNegative_ShouldThrow()
    {
        var useCase = new CreateAreaUseCase(new FakeAreaRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = "Courses",
            Slug = "courses",
            DisplayOrder = -1
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugAlreadyExistsAndIsActive_ShouldThrowConflict()
    {
        var areas = new FakeAreaRepository();
        areas.Areas.Add(TestEntityFactory.Area());
        var existingSlug = areas.Areas[0].Slug.Value;
        var useCase = new CreateAreaUseCase(areas, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = "Duplicate",
            Slug = existingSlug,
            DisplayOrder = 0
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlugAlreadyExistsAndIsInactive_ShouldThrowConflict()
    {
        var areas = new FakeAreaRepository();
        areas.Areas.Add(TestEntityFactory.Area(active: false));
        var existingSlug = areas.Areas[0].Slug.Value;
        var useCase = new CreateAreaUseCase(areas, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(new CreateAreaInput
        {
            Name = "Duplicate",
            Slug = existingSlug,
            DisplayOrder = 0
        }));
    }
}
