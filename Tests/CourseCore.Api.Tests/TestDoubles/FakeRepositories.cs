using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
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
    private readonly Dictionary<Guid, List<string>> _permissionKeysByUserId = [];

    public void AddForUser(Guid userId, Role role)
    {
        _roles[role.Id] = role;
        _rolesByUserId.TryAdd(userId, []);
        _rolesByUserId[userId].Add(role);
    }

    public void AddPermissionForUser(Guid userId, string permissionKey)
    {
        _permissionKeysByUserId.TryAdd(userId, []);
        _permissionKeysByUserId[userId].Add(permissionKey);
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

    public Task<IReadOnlyCollection<string>> FindPermissionKeysByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<string>>(
            _permissionKeysByUserId.TryGetValue(userId, out var permissions)
                ? permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : []);
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

    public int FindByLessonIdCalls { get; private set; }

    public int FindDetailsByIdCalls { get; private set; }

    public int ListCalls { get; private set; }

    public Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Courses.FirstOrDefault(course => course.Id == id));
    }

    public Task<Course?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        FindDetailsByIdCalls++;

        return FindByIdAsync(id, cancellationToken);
    }

    public Task<Course?> FindByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        FindByLessonIdCalls++;

        return Task.FromResult(Courses.FirstOrDefault(course =>
            course.Modules.Any(module => module.Lessons.Any(lesson => lesson.Id == lessonId))));
    }

    public Task<Course?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Courses.FirstOrDefault(course => course.Slug == slug));
    }

    public Task<IReadOnlyCollection<Course>> ListAsync(CancellationToken cancellationToken = default)
    {
        ListCalls++;

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

public sealed class FakeLessonRepository : ILessonRepository
{
    public List<Lesson> Lessons { get; } = [];

    public Task<Lesson?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Lessons.FirstOrDefault(lesson => lesson.Id == id));
    }

    public Task<IReadOnlyCollection<Lesson>> ListByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Lesson>>(
            Lessons.Where(lesson => lesson.ModuleId == moduleId).ToArray());
    }

    public Task<IReadOnlyCollection<Lesson>> ListByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Lesson>>([]);
    }
}

public sealed class FakeProgressRepository : IProgressRepository
{
    private readonly Dictionary<(Guid UserId, Guid LessonId), UserLessonProgress> _lessonProgresses = [];
    private readonly Dictionary<(Guid UserId, Guid CourseId), UserCourseProgress> _courseProgresses = [];

    public List<UserLessonProgress> SavedLessonProgresses { get; } = [];

    public List<UserCourseProgress> SavedCourseProgresses { get; } = [];

    public Task<UserLessonProgress?> FindLessonProgressAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        _lessonProgresses.TryGetValue((userId, lessonId), out var progress);

        return Task.FromResult(progress);
    }

    public Task<UserCourseProgress?> FindCourseProgressAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        _courseProgresses.TryGetValue((userId, courseId), out var progress);

        return Task.FromResult(progress);
    }

    public Task<IReadOnlyCollection<UserLessonProgress>> ListLessonProgressByCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<UserLessonProgress>>(
            _lessonProgresses.Values.Where(progress => progress.UserId == userId).ToArray());
    }

    public Task SaveLessonProgressAsync(
        UserLessonProgress progress,
        CancellationToken cancellationToken = default)
    {
        SavedLessonProgresses.Add(progress);
        _lessonProgresses[(progress.UserId, progress.LessonId)] = progress;

        return Task.CompletedTask;
    }

    public Task SaveCourseProgressAsync(
        UserCourseProgress progress,
        CancellationToken cancellationToken = default)
    {
        SavedCourseProgresses.Add(progress);
        _courseProgresses[(progress.UserId, progress.CourseId)] = progress;

        return Task.CompletedTask;
    }
}
