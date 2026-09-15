using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
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

        var s3Required = playbackOptions.AllowedStorageProviders.Any(provider =>
            string.Equals(provider.Trim(), "S3", StringComparison.OrdinalIgnoreCase));
        var s3Options = configuration.GetSection(S3StorageOptions.SectionName).Get<S3StorageOptions>()
            ?? new S3StorageOptions();
        S3StorageOptions.Validate(s3Options, required: s3Required);

        services.Configure<MediaPlaybackOptions>(configuration.GetSection(MediaPlaybackOptions.SectionName));
        services.Configure<S3StorageOptions>(configuration.GetSection(S3StorageOptions.SectionName));
        services.AddSingleton<IAmazonS3>(_ => CreateS3Client(s3Options));
        services.AddSingleton<IS3PresignedUrlProvider, S3PresignedUrlProvider>();
        services.AddScoped<ImageUploadService>();
        services.AddScoped<ImageUrlResolver>();

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
        services.AddScoped<RequestVideoUploadUseCase>();

        services.AddScoped<ILessonMaterialRepository, EfLessonMaterialRepository>();
        services.AddScoped<IMaterialStorageService, MaterialStorageService>();
        services.AddScoped<CreateLessonMaterialUseCase>();
        services.AddScoped<ListLessonMaterialsUseCase>();
        services.AddScoped<UpdateLessonMaterialUseCase>();
        services.AddScoped<RemoveLessonMaterialUseCase>();
        services.AddScoped<ReorderLessonMaterialsUseCase>();
        services.AddScoped<RequestLessonMaterialUploadUseCase>();
        services.AddScoped<GetLessonMaterialDownloadUrlUseCase>();

        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.AddHttpClient<IYouTubeMetadataProvider, YouTubeMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
        });
        services.AddScoped<GetYouTubeVideoMetadataUseCase>();

        return services;
    }

    private static AmazonS3Client CreateS3Client(S3StorageOptions options)
    {
        var region = string.IsNullOrWhiteSpace(options.Region)
            ? RegionEndpoint.USEast1
            : RegionEndpoint.GetBySystemName(options.Region);

        if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey), region);
        }

        return new AmazonS3Client(region);
    }
}
