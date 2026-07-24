using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Shared.Presentation.Observability;

public sealed class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            return context is null ? null : CorrelationIdConstants.GetFromItems(context);
        }
    }
}
