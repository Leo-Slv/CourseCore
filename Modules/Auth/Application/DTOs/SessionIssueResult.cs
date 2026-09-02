using CourseCore.Api.Modules.Auth.Domain.Entities;

namespace CourseCore.Api.Modules.Auth.Application.DTOs;

public sealed record SessionIssueResult(AuthOutput Output, RefreshToken RefreshToken);
