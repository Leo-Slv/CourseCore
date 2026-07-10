using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class CreateCourseUseCase
{
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCourseUseCase(ICourseRepository courses, IUnitOfWork unitOfWork)
    {
        _courses = courses;
        _unitOfWork = unitOfWork;
    }

    public Task<CourseOutput> ExecuteAsync(
        CreateCourseInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateRequired(input.Title, nameof(input.Title));
        ValidateRequired(input.Description, nameof(input.Description));

        var slug = Slug.Create(input.Slug);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            if (await _courses.ExistsBySlugAsync(slug, cancellationToken))
            {
                throw new ConflictException("A course with this slug already exists.");
            }

            var course = Course.Create(
                input.Title,
                slug,
                input.Description,
                input.DisplayOrder,
                input.ThumbnailUrl);

            foreach (var areaId in NormalizeIds(input.AreaIds))
            {
                course.AttachArea(areaId);
            }

            foreach (var moduleInput in input.Modules)
            {
                course.AddModule(CreateModule(course.Id, moduleInput));
            }

            await _courses.CreateAsync(course, cancellationToken);

            return CourseOutput.FromCourse(course);
        }, cancellationToken);
    }

    private static CourseModule CreateModule(Guid courseId, CreateCourseModuleInput input)
    {
        ValidateRequired(input.Title, nameof(input.Title));

        var module = CourseModule.Create(courseId, input.Title, input.Description, input.DisplayOrder);

        foreach (var lessonInput in input.Lessons)
        {
            module.AddLesson(CreateLesson(module.Id, lessonInput));
        }

        return module;
    }

    private static Lesson CreateLesson(Guid moduleId, CreateLessonInput input)
    {
        ValidateRequired(input.Title, nameof(input.Title));

        var lesson = Lesson.Create(moduleId, input.Title, input.Description, input.DisplayOrder);

        if (input.FreePreview)
        {
            lesson.MarkAsFreePreview();
        }

        return lesson;
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
