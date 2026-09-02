using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeCaptchaVerificationService : ICaptchaVerificationService
{
    public bool Result { get; set; } = true;

    public int Calls { get; private set; }

    public Task<bool> VerifyAsync(string captchaToken, CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(Result);
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);

public sealed class FakeEmailSender : IEmailSender
{
    public List<SentEmail> Sent { get; } = [];

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        Sent.Add(new SentEmail(to, subject, htmlBody));

        return Task.CompletedTask;
    }
}

public sealed class FakeEmailVerificationTokenHasher : IEmailVerificationTokenHasher
{
    public string Hash(string token)
    {
        return $"hash:{token}";
    }
}

public sealed class FakeEmailVerificationTokenGenerator : IEmailVerificationTokenGenerator
{
    private readonly Queue<string> _tokens;

    public FakeEmailVerificationTokenGenerator(params string[] tokens)
    {
        _tokens = new Queue<string>(tokens);
    }

    public string Generate()
    {
        return _tokens.Count > 0 ? _tokens.Dequeue() : Guid.NewGuid().ToString("N");
    }
}

public sealed class FakeEmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly Dictionary<string, EmailVerificationToken> _tokens = [];

    public List<EmailVerificationToken> Added { get; } = [];

    public void AddExisting(EmailVerificationToken token)
    {
        _tokens[token.TokenHash] = token;
    }

    public Task<EmailVerificationToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(tokenHash, out var token);

        return Task.FromResult(token);
    }

    public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        Added.Add(token);
        _tokens[token.TokenHash] = token;

        return Task.CompletedTask;
    }

    public Task<bool> TryConsumeAsync(
        Guid tokenId,
        string currentTokenHash,
        DateTime consumedAt,
        CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(currentTokenHash, out var token)
            || token.Id != tokenId
            || !token.IsActive)
        {
            return Task.FromResult(false);
        }

        token.Consume(consumedAt);

        return Task.FromResult(true);
    }

    public Task InvalidateActiveByUserIdAsync(
        Guid userId,
        DateTime consumedAt,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in _tokens.Values.Where(token => token.UserId == userId && token.IsActive))
        {
            token.Consume(consumedAt);
        }

        return Task.CompletedTask;
    }
}
