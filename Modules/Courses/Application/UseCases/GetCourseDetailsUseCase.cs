using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class GetCourseDetailsUseCase
{
    private readonly ICourseRepository _courses;

    public GetCourseDetailsUseCase(ICourseRepository courses)
    {
        _courses = courses;
    }

    public async Task<CourseDetailsOutput> ExecuteAsync(
        GetCourseDetailsInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        var course = await _courses.FindDetailsByIdAsync(input.CourseId, cancellationToken);

        if (course is null)
        {
            throw new InvalidOperationException("Course not found.");
        }

        return CourseDetailsOutput.FromCourse(course);
    }
}
