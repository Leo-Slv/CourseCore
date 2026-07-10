using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class LoginUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public LoginUseCase(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthOutput> ExecuteAsync(
        LoginInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var email = Email.Create(input.Email);
        var user = await _users.FindByEmailAsync(email, cancellationToken);

        if (user is null || !user.Active || !_passwordHasher.Verify(input.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var roles = await _roles.FindByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(role => role.Name).ToArray();
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roleNames, cancellationToken);

        return new AuthOutput
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Roles = roleNames,
            Token = new AuthToken
            {
                AccessToken = accessToken,
                RefreshToken = null,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
            }
        };
    }
}
