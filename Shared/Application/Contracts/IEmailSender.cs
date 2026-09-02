namespace CourseCore.Api.Shared.Application.Contracts;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
