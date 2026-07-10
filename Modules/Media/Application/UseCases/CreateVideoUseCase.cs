using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class CreateVideoUseCase
{
    private readonly IVideoRepository _videos;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVideoUseCase(
        IVideoRepository videos,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork)
    {
        _videos = videos;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public Task<VideoOutput> ExecuteAsync(
        CreateVideoInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(input);
        var storageProvider = ParseStorageProvider(input.StorageProvider);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

            if (lesson is null)
            {
                throw new InvalidOperationException("Lesson not found.");
            }

            var existingVideo = await _videos.FindByLessonIdAsync(input.LessonId, cancellationToken);

            if (existingVideo is not null)
            {
                throw new InvalidOperationException("Lesson already has a video.");
            }

            var video = Video.Create(
                input.LessonId,
                input.Title,
                input.Description,
                storageProvider,
                input.StorageKey,
                input.DurationSeconds,
                input.SizeBytes,
                input.ThumbnailUrl);

            if (!string.IsNullOrWhiteSpace(input.PlaybackUrl))
            {
                video.MarkAsReady(input.PlaybackUrl);
            }

            await _videos.CreateAsync(video, cancellationToken);

            return VideoOutput.FromVideo(video);
        }, cancellationToken);
    }

    private static void ValidateInput(CreateVideoInput input)
    {
        if (input.LessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new ArgumentException("Title is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.StorageProvider))
        {
            throw new ArgumentException("StorageProvider is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.StorageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(input));
        }

        if (input.DurationSeconds < 0)
        {
            throw new ArgumentException("DurationSeconds cannot be negative.", nameof(input));
        }

        if (input.SizeBytes < 0)
        {
            throw new ArgumentException("SizeBytes cannot be negative.", nameof(input));
        }
    }

    private static VideoStorageProvider ParseStorageProvider(string storageProvider)
    {
        if (Enum.TryParse<VideoStorageProvider>(storageProvider, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new ArgumentException("StorageProvider is invalid.");
    }
}
