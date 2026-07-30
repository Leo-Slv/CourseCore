using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;
    private readonly ILogger<LogoutUseCase> _logger;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs,
        ILogger<LogoutUseCase> logger)
    {
        _refreshTokens = refreshTokens;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogInformation("Logout request completed without a refresh token.");
            return;
        }

        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
        var persistedRefreshToken = await _refreshTokens.FindByTokenHashAsync(
            refreshTokenHash,
            cancellationToken);

        if (persistedRefreshToken is null)
        {
            _logger.LogInformation("Logout request completed for an unknown refresh token.");
            return;
        }

        var now = DateTime.UtcNow;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var revoked = await _refreshTokens.TryRevokeAsync(
                persistedRefreshToken.Id,
                refreshTokenHash,
                now,
                cancellationToken);

            if (!revoked)
            {
                return;
            }

            await _auditLogs.RecordAsync(
                AuditLogActionNames.LogoutSucceeded,
                "RefreshToken",
                persistedRefreshToken.Id,
                new Dictionary<string, string?> { ["result"] = "succeeded" },
                persistedRefreshToken.UserId,
                cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Logout request completed for user {UserId}.", persistedRefreshToken.UserId);
    }
}
