using System.Net;
using System.Text.Json;
using FluentAssertions;
using Lander.Helpers;
using Lander.Middleware;
using Lander.src.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace LandlordApp.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="GlobalExceptionHandlerMiddleware"/>.
///
/// Strategy: invoke the middleware with a fake <see cref="HttpContext"/> whose
/// response body is a <see cref="MemoryStream"/> so the JSON output can be
/// inspected.  The <c>next</c> delegate is a lambda that throws the exception
/// under test.
/// </summary>
public class GlobalExceptionHandlerMiddlewareTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (GlobalExceptionHandlerMiddleware middleware,
                    DefaultHttpContext httpContext,
                    MemoryStream responseBody)
        BuildMiddleware(Exception exToThrow, bool isDevelopment = false)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName)
           .Returns(isDevelopment ? "Development" : "Production");

        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();

        RequestDelegate next = _ => throw exToThrow;

        var mw = new GlobalExceptionHandlerMiddleware(next, env.Object, logger.Object);

        var body = new MemoryStream();
        var ctx  = new DefaultHttpContext();
        ctx.Response.Body = body;

        return (mw, ctx, body);
    }

    private static ErrorResponse ReadBody(MemoryStream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        var json = new StreamReader(body).ReadToEnd();
        return JsonSerializer.Deserialize<ErrorResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task NoException_DoesNotWriteToResponse()
    {
        var env    = new Mock<IWebHostEnvironment>();
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var body   = new MemoryStream();
        var ctx    = new DefaultHttpContext();
        ctx.Response.Body = body;

        RequestDelegate next = _ => Task.CompletedTask;
        var mw = new GlobalExceptionHandlerMiddleware(next, env.Object, logger.Object);

        await mw.InvokeAsync(ctx);

        body.Length.Should().Be(0);
        ctx.Response.StatusCode.Should().Be(200);
    }

    // ── NotFoundException → 404 ───────────────────────────────────────────────

    [Fact]
    public async Task NotFoundException_Returns404WithMessage()
    {
        var (mw, ctx, body) = BuildMiddleware(new NotFoundException("Apartment not found"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(404);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(404);
        resp.Message.Should().Be("Apartment not found");
    }

    // ── ConflictException → 409 ───────────────────────────────────────────────

    [Fact]
    public async Task ConflictException_Returns409WithMessage()
    {
        var (mw, ctx, body) = BuildMiddleware(new ConflictException("Duplicate entry"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(409);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(409);
        resp.Message.Should().Be("Duplicate entry");
    }

    // ── ForbiddenException → 403 ──────────────────────────────────────────────

    [Fact]
    public async Task ForbiddenException_Returns403WithMessage()
    {
        var (mw, ctx, body) = BuildMiddleware(new ForbiddenException("Access denied"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(403);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(403);
        resp.Message.Should().Be("Access denied");
    }

    // ── UnauthorizedAccessException → 401 ────────────────────────────────────

    [Fact]
    public async Task UnauthorizedAccessException_Returns401()
    {
        var (mw, ctx, body) = BuildMiddleware(new UnauthorizedAccessException());

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(401);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(401);
        resp.Message.Should().Be("Access denied.");
    }

    // ── KeyNotFoundException → 404 ────────────────────────────────────────────

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var (mw, ctx, body) = BuildMiddleware(new KeyNotFoundException());

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(404);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(404);
        resp.Message.Should().Be("Resource not found.");
    }

    // ── ArgumentException → 400 ───────────────────────────────────────────────

    [Fact]
    public async Task ArgumentException_Returns400WithMessage()
    {
        var (mw, ctx, body) = BuildMiddleware(new ArgumentException("Invalid input"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(400);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(400);
        // Message is generic on purpose — raw ArgumentException text may leak internals
        resp.Message.Should().Be("The request contains invalid data.");
    }

    // ── InvalidOperationException → 400 ──────────────────────────────────────

    [Fact]
    public async Task InvalidOperationException_Returns500WithGenericMessage()
    {
        var (mw, ctx, body) = BuildMiddleware(new InvalidOperationException("Operation not allowed"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(500);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(500);
        resp.Message.Should().Be("An error occurred while processing your request.");
    }

    // ── Generic exception → 500 ───────────────────────────────────────────────

    [Fact]
    public async Task GenericException_Returns500WithGenericMessage()
    {
        var (mw, ctx, body) = BuildMiddleware(new Exception("kaboom"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(500);
        var resp = ReadBody(body);
        resp.StatusCode.Should().Be(500);
        resp.Message.Should().Be("An unexpected error occurred.");
    }

    // ── Content-Type ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AnyException_SetsContentTypeToApplicationJson()
    {
        var (mw, ctx, _) = BuildMiddleware(new NotFoundException("x"));

        await mw.InvokeAsync(ctx);

        ctx.Response.ContentType.Should().Be("application/json");
    }

    // ── TraceId is populated ──────────────────────────────────────────────────

    [Fact]
    public async Task AnyException_IncludesTraceId()
    {
        var (mw, ctx, body) = BuildMiddleware(new NotFoundException("x"));
        ctx.TraceIdentifier = "test-trace-123";

        await mw.InvokeAsync(ctx);

        var resp = ReadBody(body);
        resp.TraceId.Should().Be("test-trace-123");
    }

    // ── Details only in Development ───────────────────────────────────────────

    [Fact]
    public async Task Production_DetailsIsNull()
    {
        var (mw, ctx, body) = BuildMiddleware(new Exception("boom"), isDevelopment: false);

        await mw.InvokeAsync(ctx);

        var resp = ReadBody(body);
        resp.Details.Should().BeNull();
    }

    [Fact]
    public async Task Development_DetailsContainsStackTrace()
    {
        var (mw, ctx, body) = BuildMiddleware(new Exception("boom"), isDevelopment: true);

        await mw.InvokeAsync(ctx);

        var resp = ReadBody(body);
        resp.Details.Should().NotBeNull();
        resp.Details.Should().Contain("System.Exception");
    }
}
