namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
