using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class RequestCourseThumbnailUploadUseCase
{
    private const string KeyPrefix = "course-thumbnails";

    private readonly ICourseRepository _courses;
    private readonly ImageUploadService _imageUploadService;
    private readonly IAuditLogService _auditLogs;

    public RequestCourseThumbnailUploadUseCase(
        ICourseRepository courses,
        ImageUploadService imageUploadService,
        IAuditLogService auditLogs)
    {
        _courses = courses;
        _imageUploadService = imageUploadService;
        _auditLogs = auditLogs;
    }

    public async Task<UploadUrlOutput> ExecuteAsync(
        Guid courseId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var course = await _courses.FindByIdAsync(courseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException("Course not found.");
        }

        var output = await _imageUploadService.RequestUploadAsync(
            KeyPrefix,
            courseId,
            fileName,
            contentType,
            sizeBytes,
            cancellationToken);

        await _auditLogs.RecordAsync(
            AuditLogActionNames.CourseThumbnailUploadRequested,
            "Course",
            courseId,
            new Dictionary<string, string?> { ["contentType"] = contentType },
            cancellationToken: cancellationToken);

        return output;
    }
}
