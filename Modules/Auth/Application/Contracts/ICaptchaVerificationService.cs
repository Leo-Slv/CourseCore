namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface ICaptchaVerificationService
{
    Task<bool> VerifyAsync(string captchaToken, CancellationToken cancellationToken = default);
}
