using System.Text.Json;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Presentation.Middleware;
using CourseCore.Api.Shared.Presentation.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace CourseCore.Api.Tests.Presentation.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationIsUnexpectedInProduction_ShouldReturnSafe500()
    {
        const string internalMessage = "internal database topology was exposed";
        var context = CreateContext();
        var middleware = CreateMiddleware(new InvalidOperationException(internalMessage));

        await middleware.InvokeAsync(context);

        var json = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", json.GetProperty("message").GetString());
        Assert.DoesNotContain(internalMessage, json.GetRawText(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        Assert.Equal("test-correlation-id", json.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenApplicationValidationFails_ShouldReturnSafe400()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(new ApplicationValidationException("Payload is invalid."));

        await middleware.InvokeAsync(context);

        var json = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Payload is invalid.", json.GetProperty("message").GetString());
        Assert.Equal("test-correlation-id", json.GetProperty("correlationId").GetString());
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(Exception exception) =>
        new(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            new TestWebHostEnvironment());

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items[CorrelationIdConstants.ItemName] = "test-correlation-id";
        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CourseCore.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
