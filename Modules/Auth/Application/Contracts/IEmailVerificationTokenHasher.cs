namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IEmailVerificationTokenHasher
{
    string Hash(string token);
}
