using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class GrantUserAreaAccessUseCase
{
    private readonly IUserRepository _users;
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;

    public GrantUserAreaAccessUseCase(
        IUserRepository users,
        IAreaRepository areas,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _areas = areas;
        _unitOfWork = unitOfWork;
    }

    public Task<AreaAccessOutput> ExecuteAsync(
        GrantUserAreaAccessInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(input.UserId, input.AreaId);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);
            var area = await _areas.FindByIdAsync(input.AreaId, cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (area is null)
            {
                throw new InvalidOperationException("Area not found.");
            }

            var access = UserAreaAccess.Create(
                input.UserId,
                input.AreaId,
                input.CanView,
                input.CanManage,
                input.StartsAt,
                input.ExpiresAt);

            await _areas.CreateUserAreaAccessAsync(access, cancellationToken);

            return new AreaAccessOutput
            {
                AreaId = access.AreaId,
                CanView = access.CanView,
                CanManage = access.CanManage
            };
        }, cancellationToken);
    }

    private static void ValidateIds(Guid userId, Guid areaId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }

        if (areaId == Guid.Empty)
        {
            throw new ArgumentException("AreaId is required.");
        }
    }
}
