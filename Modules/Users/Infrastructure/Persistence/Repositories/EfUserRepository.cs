using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Users.Infrastructure.Persistence.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfUserRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : UserMapper.ToDomain(model);
    }

    public async Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email.Value, cancellationToken);

        return model is null ? null : UserMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return models.Select(UserMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(UserMapper.ToPersistence(user), cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        UserMapper.ApplyChanges(user, model);
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(x => x.Email == email.Value, cancellationToken);
    }
}
