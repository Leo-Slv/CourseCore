using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class GetLessonMaterialDownloadUrlUseCase
{
    private readonly ILessonMaterialRepository _materials;
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly CourseAccessService _courseAccessService;
    private readonly IMaterialStorageService _materialStorageService;
    private readonly S3StorageOptions _s3Options;

    public GetLessonMaterialDownloadUrlUseCase(
        ILessonMaterialRepository materials,
        ILessonRepository lessons,
        ICourseRepository courses,
        CourseAccessService courseAccessService,
        IMaterialStorageService materialStorageService,
        IOptions<S3StorageOptions> s3Options)
    {
        _materials = materials;
        _lessons = lessons;
        _courses = courses;
        _courseAccessService = courseAccessService;
        _materialStorageService = materialStorageService;
        _s3Options = s3Options.Value;
    }

    public async Task<MaterialDownloadOutput> ExecuteAsync(
        Guid userId,
        Guid materialId,
        bool bypassAccessCheck = false,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (materialId == Guid.Empty)
        {
            throw new ArgumentException("MaterialId is required.", nameof(materialId));
        }

        var material = await _materials.FindByIdAsync(materialId, cancellationToken);

        if (material is null)
        {
            throw new NotFoundException("Lesson material not found.");
        }

        var lesson = await _lessons.FindByIdAsync(material.LessonId, cancellationToken);

        if (lesson is null)
        {
            throw new NotFoundException("Lesson not found.");
        }

        if (!bypassAccessCheck)
        {
            var course = await _courses.FindByLessonIdAsync(lesson.Id, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found for lesson.");
            }

            var access = await _courseAccessService.CanUserAccessCourseAsync(userId, course.Id, cancellationToken);

            if (!access.CanAccess && !lesson.FreePreview)
            {
                throw new ForbiddenException("User cannot access this material.");
            }
        }

        var downloadUrl = await _materialStorageService.GetDownloadUrlAsync(material, cancellationToken);

        return new MaterialDownloadOutput
        {
            MaterialId = material.Id,
            Title = material.Title,
            FileName = material.FileName,
            DownloadUrl = downloadUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_s3Options.DownloadUrlExpirationMinutes)
        };
    }
}
