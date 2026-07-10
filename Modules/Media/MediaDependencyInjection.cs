using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Storage;

namespace CourseCore.Api.Modules.Media;

public static class MediaDependencyInjection
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        services.AddScoped<IVideoRepository, EfVideoRepository>();
        services.AddScoped<IVideoStorageService, VideoStorageService>();
        services.AddScoped<CreateVideoUseCase>();
        services.AddScoped<RequestVideoPlaybackUseCase>();

        return services;
    }
}
