using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int ExecuteCalls { get; private set; }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ExecuteCalls++;
        await action();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        ExecuteCalls++;

        return await action();
    }
}
