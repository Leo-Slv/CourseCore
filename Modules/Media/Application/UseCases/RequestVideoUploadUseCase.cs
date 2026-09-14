using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class RequestVideoUploadUseCase
{
    private readonly ILessonRepository _lessons;
    private readonly IVideoStorageService _videoStorageService;
    private readonly MediaPlaybackOptions _playbackOptions;
    private readonly S3StorageOptions _s3Options;
    private readonly IAuditLogService _auditLogs;

    public RequestVideoUploadUseCase(
        ILessonRepository lessons,
        IVideoStorageService videoStorageService,
        IOptions<MediaPlaybackOptions> playbackOptions,
        IOptions<S3StorageOptions> s3Options,
        IAuditLogService auditLogs)
    {
        _lessons = lessons;
        _videoStorageService = videoStorageService;
        _playbackOptions = playbackOptions.Value;
        _s3Options = s3Options.Value;
        _auditLogs = auditLogs;
    }

    public async Task<UploadUrlOutput> ExecuteAsync(
        RequestUploadInput input,
        CancellationToken cancellationToken = default)
    {
        var provider = ParseStorageProvider(input.StorageProvider);
        Validate(input, provider);

        var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

        if (lesson is null)
        {
            throw new NotFoundException("Lesson not found.");
        }

        var storageKey = StorageKeyGenerator.Generate("videos", input.LessonId, input.FileName);
        var uploadUrl = await _videoStorageService.GetUploadUrlAsync(
            provider,
            storageKey,
            input.ContentType,
            cancellationToken);

        await _auditLogs.RecordAsync(
            AuditLogActionNames.VideoUploadRequested,
            "Lesson",
            input.LessonId,
            new Dictionary<string, string?>
            {
                ["storageProvider"] = provider.ToString(),
                ["contentType"] = input.ContentType
            },
            cancellationToken: cancellationToken);

        return new UploadUrlOutput
        {
            StorageProvider = provider.ToString(),
            StorageKey = storageKey,
            UploadUrl = uploadUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes)
        };
    }

    private void Validate(RequestUploadInput input, VideoStorageProvider provider)
    {
        if (input.LessonId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.FileName)
            || string.IsNullOrWhiteSpace(input.ContentType)
            || !MediaValidationLimits.AllowedVideoContentTypes.Contains(input.ContentType.Trim())
            || input.SizeBytes is <= 0 or > MediaValidationLimits.MaxSizeBytes)
        {
            throw new ApplicationValidationException("Video upload request payload is invalid.");
        }

        if (!_playbackOptions.AllowedStorageProviders.Any(allowed =>
            string.Equals(allowed.Trim(), provider.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ApplicationValidationException("Storage provider is not allowed.");
        }
    }

    private static VideoStorageProvider ParseStorageProvider(string storageProvider)
    {
        if (Enum.TryParse<VideoStorageProvider>(storageProvider, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new ApplicationValidationException("StorageProvider is invalid.");
    }
}
