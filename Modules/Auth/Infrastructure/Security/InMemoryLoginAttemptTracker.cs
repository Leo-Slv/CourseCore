using System.Collections.Concurrent;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

/// <summary>
/// Per-process tracker: state is lost on restart and not shared between instances, which is
/// acceptable for the single-instance deployment (a restart only grants fresh attempts).
/// </summary>
public sealed class InMemoryLoginAttemptTracker : ILoginAttemptTracker
{
    // Hard cap so a flood of random e-mails cannot grow memory without bound.
    private const int MaxTrackedAccounts = 50_000;

    private readonly ConcurrentDictionary<string, AttemptState> _states = new();
    private readonly LoginLockoutOptions _options;
    private readonly TimeProvider _time;

    public InMemoryLoginAttemptTracker(IOptions<LoginLockoutOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public TimeSpan? GetRemainingLockout(string email)
    {
        if (!_states.TryGetValue(Normalize(email), out var state))
        {
            return null;
        }

        lock (state)
        {
            var now = _time.GetUtcNow();

            if (state.LockedUntil is { } lockedUntil && lockedUntil > now)
            {
                return lockedUntil - now;
            }

            return null;
        }
    }

    public void RegisterFailure(string email)
    {
        if (_states.Count >= MaxTrackedAccounts)
        {
            RemoveExpired();
        }

        var state = _states.GetOrAdd(Normalize(email), _ => new AttemptState());
        var now = _time.GetUtcNow();
        var window = TimeSpan.FromMinutes(_options.FailureWindowMinutes);

        lock (state)
        {
            if (state.LockedUntil is { } lockedUntil && lockedUntil > now)
            {
                return;
            }

            state.Failures.RemoveAll(failure => now - failure > window);
            state.Failures.Add(now);

            if (state.Failures.Count >= Math.Max(1, _options.MaxFailedAttempts))
            {
                state.LockedUntil = now.AddMinutes(_options.LockoutMinutes);
                state.Failures.Clear();
            }
        }
    }

    public void RegisterSuccess(string email)
    {
        _states.TryRemove(Normalize(email), out _);
    }

    private void RemoveExpired()
    {
        var now = _time.GetUtcNow();
        var window = TimeSpan.FromMinutes(_options.FailureWindowMinutes);

        foreach (var (key, state) in _states)
        {
            lock (state)
            {
                var locked = state.LockedUntil is { } until && until > now;
                var hasRecentFailure = state.Failures.Any(failure => now - failure <= window);

                if (!locked && !hasRecentFailure)
                {
                    _states.TryRemove(key, out _);
                }
            }
        }
    }

    private static string Normalize(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private sealed class AttemptState
    {
        public List<DateTimeOffset> Failures { get; } = [];

        public DateTimeOffset? LockedUntil { get; set; }
    }
}
