namespace CourseCore.Api.Modules.Auth.Application.Contracts;

/// <summary>
/// Throttles password guessing per account, independent of the caller's IP address
/// (which can be rotated or forged). Any e-mail is tracked, existing or not, so the
/// behaviour does not reveal which accounts exist.
/// </summary>
public interface ILoginAttemptTracker
{
    /// <summary>Time left until the account accepts a login attempt again, or null when it is not locked.</summary>
    TimeSpan? GetRemainingLockout(string email);

    void RegisterFailure(string email);

    void RegisterSuccess(string email);
}
