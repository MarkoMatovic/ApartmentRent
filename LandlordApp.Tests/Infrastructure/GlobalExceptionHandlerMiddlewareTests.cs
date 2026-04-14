using System.IO;
using System.Text.Json;
using FluentAssertions;
using Lander.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace LandlordApp.Tests.Infrastructure;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static GlobalExceptionHandlerMiddleware CreateMiddleware(
        RequestDelegate next,
        string environmentName = "Production")
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(environmentName);

        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();

        return new GlobalExceptionHandlerMiddleware(next, mockEnv.Object, mockLogger.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var wasCalled = false;
        RequestDelegate next = _ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        wasCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        RequestDelegate next = _ => throw new Exception("Unexpected failure");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        var response = await ReadResponseAsync(context);
        response.GetProperty("statusCode").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Not allowed");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
        var response = await ReadResponseAsync(context);
        response.GetProperty("statusCode").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        RequestDelegate next = _ => throw new KeyNotFoundException("Not found");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        var response = await ReadResponseAsync(context);
        response.GetProperty("statusCode").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        RequestDelegate next = _ => throw new ArgumentException("Bad argument");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        var response = await ReadResponseAsync(context);
        response.GetProperty("statusCode").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Invalid op");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        var response = await ReadResponseAsync(context);
        response.GetProperty("statusCode").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_Production_NoStackTraceInDetails()
    {
        RequestDelegate next = _ => throw new Exception("Boom");

        var middleware = CreateMiddleware(next, environmentName: "Production");
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var response = await ReadResponseAsync(context);
        var detailsProperty = response.GetProperty("details");
        detailsProperty.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task InvokeAsync_Development_IncludesStackTrace()
    {
        RequestDelegate next = _ => throw new Exception("Boom in dev");

        var middleware = CreateMiddleware(next, environmentName: "Development");
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var response = await ReadResponseAsync(context);
        var detailsProperty = response.GetProperty("details");
        detailsProperty.ValueKind.Should().NotBe(JsonValueKind.Null);
        detailsProperty.GetString().Should().NotBeNullOrEmpty();
    }
}
