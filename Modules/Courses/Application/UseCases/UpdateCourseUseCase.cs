using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class UpdateCourseUseCase
{
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateCourseUseCase(
        ICourseRepository courses,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _courses = courses;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<CourseOutput> ExecuteAsync(
        UpdateCourseInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        ValidateRequired(input.Title, nameof(input.Title));
        ValidateRequired(input.Description, nameof(input.Description));

        var slug = Slug.Create(input.Slug);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var course = await _courses.FindDetailsByIdAsync(input.CourseId, cancellationToken)
                ?? await _courses.FindByIdAsync(input.CourseId, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found.");
            }

            if (course.Slug != slug && await _courses.ExistsBySlugAsync(slug, cancellationToken))
            {
                throw new ConflictException("A course with this slug already exists.");
            }

            course.ChangeTitle(input.Title);
            course.ChangeSlug(slug);
            course.ChangeDescription(input.Description);
            course.ChangeThumbnailUrl(input.ThumbnailUrl);
            course.ChangeDisplayOrder(input.DisplayOrder);

            SyncAreas(course.AreaIds, NormalizeIds(input.AreaIds), course.AttachArea, course.DetachArea);

            await _courses.UpdateAsync(course, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.CourseUpdated,
                "Course",
                course.Id,
                cancellationToken: cancellationToken);

            return CourseOutput.FromCourse(course);
        }, cancellationToken);
    }

    private static void SyncAreas(
        IReadOnlyCollection<Guid> currentAreaIds,
        IReadOnlyCollection<Guid> requestedAreaIds,
        Action<Guid> attachArea,
        Action<Guid> detachArea)
    {
        var current = currentAreaIds.ToHashSet();
        var requested = requestedAreaIds.ToHashSet();

        foreach (var areaId in requested.Except(current))
        {
            attachArea(areaId);
        }

        foreach (var areaId in current.Except(requested))
        {
            detachArea(areaId);
        }
    }

    private static IReadOnlyCollection<Guid> NormalizeIds(IReadOnlyCollection<Guid> ids)
    {
        return ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private static void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.");
        }
    }
}
