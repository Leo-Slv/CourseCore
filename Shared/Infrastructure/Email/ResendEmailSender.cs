using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CourseCore.Api.Shared.Application.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Shared.Infrastructure.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<ResendOptions> options,
        IHostEnvironment environment,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            if (_environment.IsProduction())
            {
                throw new InvalidOperationException("Resend:ApiKey is not configured.");
            }

            _logger.LogWarning(
                "Resend:ApiKey is not configured; skipping email delivery to {Recipient} outside Production.",
                to);
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new ResendEmailPayload
            {
                From = string.IsNullOrWhiteSpace(_options.FromName)
                    ? _options.FromAddress
                    : $"{_options.FromName} <{_options.FromAddress}>",
                To = [to],
                Subject = subject,
                Html = htmlBody
            })
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Resend rejected an email to {Recipient} with status {StatusCode}: {Body}",
                to,
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException("Failed to send email via Resend.");
        }
    }

    private sealed class ResendEmailPayload
    {
        [JsonPropertyName("from")]
        public string From { get; init; } = string.Empty;

        [JsonPropertyName("to")]
        public IReadOnlyCollection<string> To { get; init; } = [];

        [JsonPropertyName("subject")]
        public string Subject { get; init; } = string.Empty;

        [JsonPropertyName("html")]
        public string Html { get; init; } = string.Empty;
    }
}
