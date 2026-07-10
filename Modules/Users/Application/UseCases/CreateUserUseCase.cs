using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class CreateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public Task<UserOutput> ExecuteAsync(
        CreateUserInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new ArgumentException("Name is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new ArgumentException("Password is required.", nameof(input));
        }

        var email = Email.Create(input.Email);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            if (await _users.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var passwordHash = _passwordHasher.Hash(input.Password);
            var user = User.Create(input.Name, email, passwordHash);

            await _users.CreateAsync(user, cancellationToken);

            return UserOutput.FromUser(user);
        }, cancellationToken);
    }
}
