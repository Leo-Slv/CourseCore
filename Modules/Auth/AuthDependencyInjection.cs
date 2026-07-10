namespace CourseCore.Api.Modules.Auth;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
