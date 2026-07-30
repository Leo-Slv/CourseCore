using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Application.Options;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Progress;

public static class ProgressDependencyInjection
{
    public static IServiceCollection AddProgressModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var progressOptions = configuration.GetSection(ProgressOptions.SectionName).Get<ProgressOptions>()
            ?? new ProgressOptions();
        ProgressOptions.Validate(progressOptions);

        services.Configure<ProgressOptions>(configuration.GetSection(ProgressOptions.SectionName));
        services.AddScoped<IProgressRepository, EfProgressRepository>();
        services.AddScoped<RegisterLessonProgressUseCase>();
        services.AddScoped<GetCourseProgressUseCase>();

        return services;
    }
}
