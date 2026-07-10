using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Services;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class CheckCourseAccessUseCase
{
    private readonly CourseAccessService _courseAccessService;

    public CheckCourseAccessUseCase(CourseAccessService courseAccessService)
    {
        _courseAccessService = courseAccessService;
    }

    public Task<CourseAccessOutput> ExecuteAsync(
        CheckCourseAccessInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        return _courseAccessService.CanUserAccessCourseAsync(
            input.UserId,
            input.CourseId,
            cancellationToken);
    }
}
