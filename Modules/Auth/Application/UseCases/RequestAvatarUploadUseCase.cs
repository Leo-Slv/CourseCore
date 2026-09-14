using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class RequestAvatarUploadUseCase
{
    private const string KeyPrefix = "avatars";

    private readonly IUserRepository _users;
    private readonly ImageUploadService _imageUploadService;
    private readonly IAuditLogService _auditLogs;

    public RequestAvatarUploadUseCase(
        IUserRepository users,
        ImageUploadService imageUploadService,
        IAuditLogService auditLogs)
    {
        _users = users;
        _imageUploadService = imageUploadService;
        _auditLogs = auditLogs;
    }

    public async Task<ImageUploadUrlOutput> ExecuteAsync(
        Guid userId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        var output = await _imageUploadService.RequestUploadAsync(
            KeyPrefix,
            userId,
            fileName,
            contentType,
            sizeBytes,
            cancellationToken);

        await _auditLogs.RecordAsync(
            AuditLogActionNames.AvatarUploadRequested,
            "User",
            userId,
            new Dictionary<string, string?> { ["contentType"] = contentType },
            userId: userId,
            cancellationToken: cancellationToken);

        return output;
    }
}
