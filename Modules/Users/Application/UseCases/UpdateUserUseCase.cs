using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class UpdateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserUseCase(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public Task<UserOutput> ExecuteAsync(
        UpdateUserInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        var email = Email.Create(input.Email);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (user.Email != email && await _users.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            user.ChangeName(input.Name);

            if (user.Email != email)
            {
                user.ChangeEmail(email);
            }

            if (input.Active)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }

            await _users.UpdateAsync(user, cancellationToken);

            return UserOutput.FromUser(user);
        }, cancellationToken);
    }
}
