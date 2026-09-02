namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IEmailVerificationTokenGenerator
{
    string Generate();
}
