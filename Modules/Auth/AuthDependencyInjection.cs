using System.Text;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
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

            options.AddPolicy(AuthPolicyNames.ManageUsers, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthRoleNames.Admin);
            });

            options.AddPolicy(AuthPolicyNames.ManageAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthRoleNames.Admin);
            });

            options.AddPolicy(AuthPolicyNames.ManageCourses, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthRoleNames.Admin);
            });

            options.AddPolicy(AuthPolicyNames.ManageVideos, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthRoleNames.Admin);
            });

            options.AddPolicy(AuthPolicyNames.ReadProgress, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthRoleNames.Admin);
            });
        });

        return services;
    }
}
