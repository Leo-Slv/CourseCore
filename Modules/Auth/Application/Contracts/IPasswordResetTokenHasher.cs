namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IPasswordResetTokenHasher
{
    string Hash(string token);
}
