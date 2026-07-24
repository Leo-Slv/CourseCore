using System.Text;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CourseCore.Api.Modules.Auth;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        JwtTokenService.ValidateOptions(jwtOptions);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
        services.AddScoped<IRefreshTokenGenerator, SecureRefreshTokenGenerator>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicyNames.AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthRoleNames.Admin);
            });

            AddPermissionPolicy(options, AuthPolicyNames.ManageUsers, AuthPermissionNames.ManageUsers);
            AddPermissionPolicy(
                options,
                AuthPolicyNames.ManageAccess,
                AuthPermissionNames.ManageAreas,
                AuthPermissionNames.ManageRoles);
            AddPermissionPolicy(options, AuthPolicyNames.ManageCourses, AuthPermissionNames.ManageCourses);
            AddPermissionPolicy(options, AuthPolicyNames.ManageVideos, AuthPermissionNames.ManageVideos);
            AddPermissionPolicy(options, AuthPolicyNames.ReadProgress, AuthPermissionNames.ReadProgress);
        });

        return services;
    }

    private static void AddPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        params string[] permissions)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => HasPermissionOrAdmin(context, permissions));
        });
    }

    private static bool HasPermissionOrAdmin(AuthorizationHandlerContext context, IReadOnlyCollection<string> permissions)
    {
        return context.User.IsInRole(AuthRoleNames.Admin)
            || permissions.Any(permission => context.User.HasClaim(AuthClaimTypes.Permission, permission));
    }
}
