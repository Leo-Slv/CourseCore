namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IPasswordResetTokenGenerator
{
    string Generate();
}
