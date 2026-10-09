using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Application.DTOs;
using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Payments.Application.UseCases;

public class CreateCheckoutUseCase
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IOrderRepository _orders;
    private readonly CourseAccessService _courseAccessService;
    private readonly IStripeGateway _stripeGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;
    private readonly FrontendOptions _frontendOptions;

    public CreateCheckoutUseCase(
        IUserRepository users,
        ICourseRepository courses,
        IOrderRepository orders,
        CourseAccessService courseAccessService,
        IStripeGateway stripeGateway,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs,
        IOptions<FrontendOptions> frontendOptions)
    {
        _users = users;
        _courses = courses;
        _orders = orders;
        _courseAccessService = courseAccessService;
        _stripeGateway = stripeGateway;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task<CheckoutOutput> ExecuteAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(courseId));
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!user.Active || !user.EmailVerifiedAt.HasValue)
        {
            throw new ForbiddenException("User cannot purchase courses.");
        }

        var course = await _courses.FindByIdAsync(courseId, cancellationToken);

        if (course is null || !course.Published)
        {
            throw new NotFoundException("Course not found.");
        }

        if (course.PricingModel != CoursePricingModel.Paid)
        {
            throw new ConflictException("Course is not available for purchase.");
        }

        if (course.PriceAmount is null or <= 0)
        {
            throw new ConflictException("Course has no price configured.");
        }

        var access = await _courseAccessService.CanUserAccessCourseAsync(userId, courseId, cancellationToken);

        if (access.CanAccess)
        {
            throw new ConflictException("User already has access to this course.");
        }

        if (access.Reason is CourseAccessDenialReasons.CourseHasNoLinkedAreas
            or CourseAccessDenialReasons.CourseHasNoActiveLinkedAreas)
        {
            throw new ConflictException("Course is not available for purchase.");
        }

        var amountInCents = (long)decimal.Round(course.PriceAmount.Value * 100m, 0, MidpointRounding.AwayFromZero);
        var order = Order.Create(userId, courseId, amountInCents);

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _orders.CreateAsync(order, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.OrderCreated,
                "Order",
                order.Id,
                new Dictionary<string, string?>
                {
                    ["courseId"] = courseId.ToString(),
                    ["amountInCents"] = amountInCents.ToString(),
                    ["displayName"] = $"{user.Email.Value} → {course.Title}"
                },
                userId: userId,
                cancellationToken: cancellationToken);
        }, cancellationToken);

        CheckoutSessionResult session;

        try
        {
            var baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
            session = await _stripeGateway.CreateCheckoutSessionAsync(
                new CreateCheckoutSessionRequest(
                    order.Id,
                    userId,
                    courseId,
                    course.Title,
                    amountInCents,
                    order.Currency,
                    user.Email.Value,
                    $"{baseUrl}/checkout/success?orderId={order.Id}",
                    $"{baseUrl}/checkout/cancel?orderId={order.Id}"),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await _unitOfWork.ExecuteAsync(async () =>
            {
                order.MarkFailed();
                await _orders.UpdateAsync(order, cancellationToken);
            }, CancellationToken.None);

            throw new ExternalServiceException("Payment provider is unavailable.", exception);
        }

        await _unitOfWork.ExecuteAsync(async () =>
        {
            order.AttachCheckoutSession(session.SessionId);
            await _orders.UpdateAsync(order, cancellationToken);
        }, cancellationToken);

        return new CheckoutOutput { OrderId = order.Id, CheckoutUrl = session.Url };
    }
}
