namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}
