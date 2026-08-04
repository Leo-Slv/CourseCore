namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface IPasswordPolicy
{
    void Validate(string? password);
}
