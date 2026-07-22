using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _usersById = [];
    private readonly Dictionary<string, User> _usersByEmail = [];

    public void Add(User user)
    {
        _usersById[user.Id] = user;
        _usersByEmail[user.Email.Value] = user;
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _usersById.TryGetValue(id, out var user);

        return Task.FromResult(user);
    }

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        _usersByEmail.TryGetValue(email.Value, out var user);

        return Task.FromResult(user);
    }

    public Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<User>>(_usersById.Values.ToArray());
    }

    public Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        Add(user);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        Add(user);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_usersByEmail.ContainsKey(email.Value));
    }
}

public sealed class FakeRoleRepository : IRoleRepository
{
    private readonly Dictionary<Guid, Role> _roles = [];
    private readonly Dictionary<Guid, List<Role>> _rolesByUserId = [];

    public void AddForUser(Guid userId, Role role)
    {
        _roles[role.Id] = role;
        _rolesByUserId.TryAdd(userId, []);
        _rolesByUserId[userId].Add(role);
    }

    public Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _roles.TryGetValue(id, out var role);

        return Task.FromResult(role);
    }

    public Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_roles.Values.FirstOrDefault(role => role.Name == name));
    }

    public Task<IReadOnlyCollection<Role>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Role>>(
            _rolesByUserId.TryGetValue(userId, out var roles) ? roles.ToArray() : []);
    }

    public Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Role>>(_roles.Values.ToArray());
    }

    public Task CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles[role.Id] = role;

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles[role.Id] = role;

        return Task.CompletedTask;
    }
}

public sealed class FakeAreaRepository : IAreaRepository
{
    public List<Area> Areas { get; } = [];

    public List<UserAreaAccess> UserAreaAccesses { get; } = [];

    public List<RoleAreaAccess> RoleAreaAccesses { get; } = [];

    public Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Areas.FirstOrDefault(area => area.Id == id));
    }

    public Task<Area?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Areas.FirstOrDefault(area => area.Slug == slug));
    }

    public Task<IReadOnlyCollection<Area>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Area>>(Areas.ToArray());
    }

    public Task<IReadOnlyCollection<Area>> ListByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var areaIds = UserAreaAccesses.Where(access => access.UserId == userId).Select(access => access.AreaId).ToHashSet();

        return Task.FromResult<IReadOnlyCollection<Area>>(Areas.Where(area => areaIds.Contains(area.Id)).ToArray());
    }

    public Task CreateAsync(Area area, CancellationToken cancellationToken = default)
    {
        Areas.Add(area);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Area area, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CreateUserAreaAccessAsync(UserAreaAccess access, CancellationToken cancellationToken = default)
    {
        UserAreaAccesses.Add(access);

        return Task.CompletedTask;
    }

    public Task CreateRoleAreaAccessAsync(RoleAreaAccess access, CancellationToken cancellationToken = default)
    {
        RoleAreaAccesses.Add(access);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<UserAreaAccess>> ListUserAreaAccessesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<UserAreaAccess>>(
            UserAreaAccesses.Where(access => access.UserId == userId).ToArray());
    }

    public Task<IReadOnlyCollection<RoleAreaAccess>> ListRoleAreaAccessesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var roleIdSet = roleIds.ToHashSet();

        return Task.FromResult<IReadOnlyCollection<RoleAreaAccess>>(
            RoleAreaAccesses.Where(access => roleIdSet.Contains(access.RoleId)).ToArray());
    }
}

public sealed class FakeCourseRepository : ICourseRepository
{
    public List<Course> Courses { get; } = [];

    public Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Courses.FirstOrDefault(course => course.Id == id));
    }

    public Task<Course?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return FindByIdAsync(id, cancellationToken);
    }

    public Task<Course?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Courses.FirstOrDefault(course => course.Slug == slug));
    }

    public Task<IReadOnlyCollection<Course>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Course>>(Courses.ToArray());
    }

    public Task<IReadOnlyCollection<Course>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Course>>(Courses.Where(course => course.Published).ToArray());
    }

    public Task<IReadOnlyCollection<Course>> ListByAreaIdsAsync(
        IReadOnlyCollection<Guid> areaIds,
        CancellationToken cancellationToken = default)
    {
        var areaIdSet = areaIds.ToHashSet();

        return Task.FromResult<IReadOnlyCollection<Course>>(
            Courses.Where(course => course.AreaIds.Any(areaIdSet.Contains)).ToArray());
    }

    public Task CreateAsync(Course course, CancellationToken cancellationToken = default)
    {
        Courses.Add(course);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Course course, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Courses.Any(course => course.Slug == slug));
    }
}
