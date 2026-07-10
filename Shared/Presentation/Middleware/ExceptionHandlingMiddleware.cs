using System.Diagnostics;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CourseCore.Api.Shared.Presentation.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var error = MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception handled by middleware. TraceId: {TraceId}",
            Activity.Current?.Id ?? context.TraceIdentifier);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = error.StatusCode;

        var response = new ApiErrorResponse
        {
            StatusCode = error.StatusCode,
            Error = error.Title,
            Message = GetMessage(exception, error),
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static ErrorDescriptor MapException(Exception exception)
    {
        return exception switch
        {
            DomainException => new ErrorDescriptor(
                StatusCodes.Status400BadRequest,
                "Bad Request"),
            ArgumentException => new ErrorDescriptor(
                StatusCodes.Status400BadRequest,
                "Bad Request"),
            InvalidOperationException => new ErrorDescriptor(
                StatusCodes.Status400BadRequest,
                "Bad Request"),
            UnauthorizedAccessException => new ErrorDescriptor(
                StatusCodes.Status401Unauthorized,
                "Unauthorized"),
            KeyNotFoundException => new ErrorDescriptor(
                StatusCodes.Status404NotFound,
                "Not Found"),
            NotSupportedException => new ErrorDescriptor(
                StatusCodes.Status501NotImplemented,
                "Not Implemented"),
            _ => new ErrorDescriptor(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error")
        };
    }

    private string GetMessage(Exception exception, ErrorDescriptor error)
    {
        if (error.StatusCode == StatusCodes.Status500InternalServerError &&
            !_environment.IsDevelopment())
        {
            return "An unexpected error occurred.";
        }

        return exception.Message;
    }

    private sealed record ErrorDescriptor(int StatusCode, string Title);
}
