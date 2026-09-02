using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class LoginUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;
    private readonly ILogger<LoginUseCase> _logger;

    public LoginUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokens,
        SessionIssuer sessionIssuer,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs,
        ILogger<LoginUseCase> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _sessionIssuer = sessionIssuer;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        _logger = logger;
    }

    public async Task<AuthOutput> ExecuteAsync(
        LoginInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            _logger.LogWarning("Login attempt rejected because credentials were incomplete.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var email = Email.Create(input.Email);
        var user = await _users.FindByEmailAsync(email, cancellationToken);
        var passwordIsValid = user is null
            ? _passwordHasher.VerifyDummy(input.Password)
            : _passwordHasher.Verify(input.Password, user.PasswordHash);

        if (user is null || !user.Active || !passwordIsValid)
        {
            _logger.LogWarning("Login attempt rejected with invalid credentials.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var session = await _sessionIssuer.BuildAsync(user, cancellationToken);

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _refreshTokens.AddAsync(session.RefreshToken, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LoginSucceeded,
                "User",
                user.Id,
                new Dictionary<string, string?> { ["result"] = "succeeded" },
                user.Id,
                cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("User {UserId} signed in successfully.", user.Id);

        return session.Output;
    }
}
