using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Repositories;
using CourseCore.Api.Modules.Media.Infrastructure.Storage;
using CourseCore.Api.Modules.Media.Infrastructure.YouTube;

namespace CourseCore.Api.Modules.Media;

public static class MediaDependencyInjection
{
    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var playbackOptions = configuration.GetSection(MediaPlaybackOptions.SectionName).Get<MediaPlaybackOptions>()
            ?? new MediaPlaybackOptions();
        MediaPlaybackOptions.Validate(playbackOptions, requireSigningSecret: environment.IsProduction());

        services.Configure<MediaPlaybackOptions>(configuration.GetSection(MediaPlaybackOptions.SectionName));
        services.AddScoped<IVideoRepository, EfVideoRepository>();
        services.AddScoped<IVideoStorageService, VideoStorageService>();
        services.AddScoped<CreateVideoUseCase>();
        services.AddScoped<MarkVideoReadyUseCase>();
        services.AddScoped<RequestVideoPlaybackUseCase>();
        services.AddScoped<GetLessonVideoUseCase>();
        services.AddScoped<ReplaceLessonVideoUseCase>();
        services.AddScoped<RemoveLessonVideoUseCase>();
        services.AddScoped<ListVideosUseCase>();
        services.AddScoped<ActivateVideoUseCase>();
        services.AddScoped<UnlistVideoUseCase>();

        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.AddHttpClient<IYouTubeMetadataProvider, YouTubeMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
        });
        services.AddScoped<GetYouTubeVideoMetadataUseCase>();

        return services;
    }
}
