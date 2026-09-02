using System.Text.Json.Serialization;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class TurnstileCaptchaVerificationService : ICaptchaVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TurnstileCaptchaVerificationService> _logger;

    public TurnstileCaptchaVerificationService(
        HttpClient httpClient,
        IOptions<TurnstileOptions> options,
        IHostEnvironment environment,
        ILogger<TurnstileCaptchaVerificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string captchaToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            if (_environment.IsProduction())
            {
                throw new InvalidOperationException("Turnstile:SecretKey is not configured.");
            }

            _logger.LogWarning("Turnstile:SecretKey is not configured; bypassing CAPTCHA verification outside Production.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(captchaToken))
        {
            return false;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = captchaToken
        });

        using var response = await _httpClient.PostAsync("turnstile/v0/siteverify", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Turnstile verification request failed with status {StatusCode}.", (int)response.StatusCode);
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<TurnstileVerificationResponse>(cancellationToken);

        return result?.Success ?? false;
    }

    private sealed class TurnstileVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
    }
}
