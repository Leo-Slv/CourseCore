using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Progress;

public static class ProgressDependencyInjection
{
    public static IServiceCollection AddProgressModule(this IServiceCollection services)
    {
        services.AddScoped<IProgressRepository, EfProgressRepository>();
        services.AddScoped<RegisterLessonProgressUseCase>();
        services.AddScoped<GetCourseProgressUseCase>();

        return services;
    }
}
