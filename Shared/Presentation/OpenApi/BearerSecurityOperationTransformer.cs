using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CourseCore.Api.Shared.Presentation.OpenApi;

public sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    private const string SecuritySchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (AllowsAnonymous(context))
        {
            return Task.CompletedTask;
        }

        if (!RequiresAuthorization(context))
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SecuritySchemeName, context.Document)] = []
        });

        return Task.CompletedTask;
    }

    private static bool AllowsAnonymous(OpenApiOperationTransformerContext context)
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (endpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return true;
        }

        return context.Description.ActionDescriptor is ControllerActionDescriptor actionDescriptor &&
            (actionDescriptor.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any() ||
             actionDescriptor.ControllerTypeInfo.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any());
    }

    private static bool RequiresAuthorization(OpenApiOperationTransformerContext context)
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (endpointMetadata.OfType<IAuthorizeData>().Any())
        {
            return true;
        }

        return context.Description.ActionDescriptor is ControllerActionDescriptor actionDescriptor &&
            (actionDescriptor.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any() ||
             actionDescriptor.ControllerTypeInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any());
    }
}
