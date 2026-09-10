using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Xml;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Infrastructure.YouTube;

public sealed class YouTubeMetadataProvider : IYouTubeMetadataProvider
{
    private readonly HttpClient _httpClient;
    private readonly YouTubeOptions _options;
    private readonly ILogger<YouTubeMetadataProvider> _logger;

    public YouTubeMetadataProvider(
        HttpClient httpClient,
        IOptions<YouTubeOptions> options,
        ILogger<YouTubeMetadataProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<YouTubeVideoMetadata?> GetMetadataAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning(
                "YouTube:ApiKey is not configured; cannot resolve metadata for video {VideoId}.",
                videoId);
            return null;
        }

        var requestUri =
            $"videos?part=contentDetails,snippet&id={Uri.EscapeDataString(videoId)}&key={Uri.EscapeDataString(_options.ApiKey)}";

        var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "YouTube Data API rejected a metadata lookup for {VideoId} with status {StatusCode}: {Body}",
                videoId,
                (int)response.StatusCode,
                body);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<YouTubeVideosListResponse>(cancellationToken);
        var item = payload?.Items.FirstOrDefault();

        if (item is null)
        {
            return null;
        }

        var durationSeconds = ParseIso8601Duration(item.ContentDetails.Duration);
        var thumbnailUrl = item.Snippet.Thumbnails.High?.Url
            ?? item.Snippet.Thumbnails.Default?.Url
            ?? string.Empty;

        return new YouTubeVideoMetadata
        {
            Title = item.Snippet.Title,
            ThumbnailUrl = thumbnailUrl,
            DurationSeconds = durationSeconds
        };
    }

    private static int ParseIso8601Duration(string iso8601Duration)
    {
        try
        {
            return (int)XmlConvert.ToTimeSpan(iso8601Duration).TotalSeconds;
        }
        catch (FormatException)
        {
            return 0;
        }
    }

    private sealed class YouTubeVideosListResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeVideoItem> Items { get; init; } = [];
    }

    private sealed class YouTubeVideoItem
    {
        [JsonPropertyName("snippet")]
        public YouTubeVideoSnippet Snippet { get; init; } = new();

        [JsonPropertyName("contentDetails")]
        public YouTubeContentDetails ContentDetails { get; init; } = new();
    }

    private sealed class YouTubeVideoSnippet
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnails Thumbnails { get; init; } = new();
    }

    private sealed class YouTubeThumbnails
    {
        [JsonPropertyName("default")]
        public YouTubeThumbnail? Default { get; init; }

        [JsonPropertyName("high")]
        public YouTubeThumbnail? High { get; init; }
    }

    private sealed class YouTubeThumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; init; } = string.Empty;
    }

    private sealed class YouTubeContentDetails
    {
        [JsonPropertyName("duration")]
        public string Duration { get; init; } = "PT0S";
    }
}
