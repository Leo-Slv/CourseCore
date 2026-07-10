using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class PublishCourseUseCase
{
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public PublishCourseUseCase(ICourseRepository courses, IUnitOfWork unitOfWork)
    {
        _courses = courses;
        _unitOfWork = unitOfWork;
    }

    public Task<CourseOutput> ExecuteAsync(
        PublishCourseInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var course = await _courses.FindDetailsByIdAsync(input.CourseId, cancellationToken)
                ?? await _courses.FindByIdAsync(input.CourseId, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found.");
            }

            course.Publish();

            await _courses.UpdateAsync(course, cancellationToken);

            return CourseOutput.FromCourse(course);
        }, cancellationToken);
    }
}
