using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Entities;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<string, RefreshToken> _tokens = [];

    public List<RefreshToken> Added { get; } = [];

    public List<RefreshToken> Updated { get; } = [];

    public bool ForceRotateFailure { get; set; }

    public void AddExisting(RefreshToken refreshToken)
    {
        _tokens[refreshToken.TokenHash] = refreshToken;
    }

    public Task<RefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(tokenHash, out var refreshToken);

        return Task.FromResult(refreshToken);
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Added.Add(refreshToken);
        _tokens[refreshToken.TokenHash] = refreshToken;

        return Task.CompletedTask;
    }

    public Task<bool> TryRotateAsync(
        Guid refreshTokenId,
        string currentTokenHash,
        string replacementTokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        if (ForceRotateFailure
            || !_tokens.TryGetValue(currentTokenHash, out var refreshToken)
            || refreshToken.Id != refreshTokenId
            || !refreshToken.IsActive
            || refreshToken.ExpiresAt <= revokedAt)
        {
            return Task.FromResult(false);
        }

        refreshToken.Revoke(replacementTokenHash, revokedAt);

        return Task.FromResult(true);
    }

    public Task<bool> TryRevokeAsync(
        Guid refreshTokenId,
        string currentTokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(currentTokenHash, out var refreshToken)
            || refreshToken.Id != refreshTokenId
            || !refreshToken.IsActive
            || refreshToken.ExpiresAt <= revokedAt)
        {
            return Task.FromResult(false);
        }

        refreshToken.Revoke(revokedAt: revokedAt);

        return Task.FromResult(true);
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Updated.Add(refreshToken);
        _tokens[refreshToken.TokenHash] = refreshToken;

        return Task.CompletedTask;
    }
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return $"hashed:{password}";
    }

    public bool Verify(string password, string passwordHash)
    {
        return passwordHash == Hash(password);
    }
}

public sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        return $"hash:{refreshToken}";
    }
}

public sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly Queue<string> _tokens;

    public FakeRefreshTokenGenerator(params string[] tokens)
    {
        _tokens = new Queue<string>(tokens);
    }

    public string Generate()
    {
        return _tokens.Count > 0 ? _tokens.Dequeue() : Guid.NewGuid().ToString("N");
    }
}

public sealed class FakeTokenService : ITokenService
{
    private int _calls;

    public int Calls => _calls;

    public Task<string> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        _calls++;

        return Task.FromResult($"access-token-{_calls}");
    }
}
