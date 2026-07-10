using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Media;

public static class MediaDependencyInjection
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        services.AddScoped<IVideoRepository, EfVideoRepository>();

        return services;
    }
}
