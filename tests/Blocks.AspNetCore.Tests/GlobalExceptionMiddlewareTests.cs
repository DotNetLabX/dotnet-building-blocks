using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Blocks.Domain;
using Blocks.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Blocks.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Blocks.AspNetCore.Tests;

public sealed class GlobalExceptionMiddlewareTests
{
    public static TheoryData<string, int> StatusPerType => new()
    {
        { nameof(ArgumentException), 400 },
        { nameof(BadRequestException), 400 },
        { nameof(DomainException), 400 },
        { nameof(UnauthorizedException), 401 },
        { nameof(ForbiddenException), 403 },
        { nameof(NotFoundException), 404 },
        { nameof(ConflictException), 409 },
        { nameof(InvalidOperationException), 500 },
        { nameof(BadGatewayException), 502 },
    };

    [Theory]
    [MemberData(nameof(StatusPerType))]
    public async Task EachErrorType_AnswersItsStatus(string type, int expected)
    {
        var reply = await ErrorMapperHost.SendAsync(Create(type));

        ((int)reply.Status).Should().Be(expected);
    }

    public static TheoryData<string, bool> LoggedPerType => new()
    {
        { nameof(NotFoundException), false },
        { nameof(InvalidOperationException), true },
        { nameof(BadGatewayException), true },
    };

    [Theory]
    [MemberData(nameof(LoggedPerType))]
    public async Task OnlyAStatusOf500AndAbove_IsLoggedAsAnError(string type, bool logged)
    {
        var logs = new CapturedLogs();

        await ErrorMapperHost.SendAsync(Create(type), logs: logs);

        logs.Entries.Any(e => e.Level == LogLevel.Error && e.Category.EndsWith(nameof(GlobalExceptionMiddleware), StringComparison.Ordinal) && e.Exception?.GetType().Name == type)
            .Should().Be(logged);
    }

    [Fact]
    public async Task ValidationError_Answers400()
    {
        var reply = await ErrorMapperHost.SendAsync(Invalid());

        reply.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancellation_Answers499()
    {
        var logs = new CapturedLogs();

        var reply = await ErrorMapperHost.SendAsync(new OperationCanceledException(), logs: logs, clientAborted: true);

        ((int)reply.Status).Should().Be(499);
        logs.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task CancellationWithoutClientAbort_IsNot499()
    {
        var logs = new CapturedLogs();

        var reply = await ErrorMapperHost.SendAsync(new OperationCanceledException(), logs: logs);

        reply.Status.Should().Be(HttpStatusCode.InternalServerError);
        logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Exception is OperationCanceledException);
    }

    [Fact]
    public async Task UpstreamTimeoutWrappedAsBadGateway_Answers502()
    {
        var logs = new CapturedLogs();
        var timeout = new BadGatewayException("upstream timed out", new TaskCanceledException());

        var reply = await ErrorMapperHost.SendAsync(timeout, logs: logs);

        reply.Status.Should().Be(HttpStatusCode.BadGateway);
        logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Exception == timeout);
    }

    [Fact]
    public async Task UnlistedHttpError_AnswersItsOwnStatus()
    {
        var reply = await ErrorMapperHost.SendAsync(new TeapotException());

        ((int)reply.Status).Should().Be(418);
    }

    [Fact]
    public async Task CancellationWrappedInAnotherError_Answers499()
    {
        var logs = new CapturedLogs();
        var wrapped = new InvalidOperationException("outer", new TaskCanceledException());

        var reply = await ErrorMapperHost.SendAsync(wrapped, logs: logs, clientAborted: true);

        ((int)reply.Status).Should().Be(499);
        logs.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task CancellationAfterTheResponseStarted_LeavesTheStatus()
    {
        var reply = await ErrorMapperHost.SendAsync(async context =>
        {
            context.Response.StatusCode = 202;
            await context.Response.WriteAsync("partial", CancellationToken.None);
            await context.Response.Body.FlushAsync(CancellationToken.None);
            throw new InvalidOperationException("outer", new OperationCanceledException());
        }, clientAborted: true);

        reply.Status.Should().Be(HttpStatusCode.Accepted);
        reply.Body.Should().Be("partial");
    }

    [Fact]
    public async Task ServerErrorOutsideDevelopment_CarriesTheFixedTextOnly()
    {
        var reply = await ErrorMapperHost.SendAsync(new InvalidOperationException("connection string leaked"));

        reply.Status.Should().Be(HttpStatusCode.InternalServerError);
        reply.Json.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
        reply.Body.Should().NotContain("connection string leaked");
        reply.Json.GetProperty("details").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task BadGatewayOutsideDevelopment_CarriesItsOwnMessage()
    {
        var reply = await ErrorMapperHost.SendAsync(new BadGatewayException("upstream failed"));

        reply.Json.GetProperty("message").GetString().Should().Be("upstream failed");
    }

    [Fact]
    public async Task ServerErrorInDevelopment_CarriesDetails()
    {
        var reply = await ErrorMapperHost.SendAsync(new InvalidOperationException("internal state"), "Development");

        reply.Status.Should().Be(HttpStatusCode.InternalServerError);
        reply.Json.GetProperty("message").GetString().Should().Be("internal state");
        reply.Json.GetProperty("details").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidationErrorInDevelopment_CarriesDetails()
    {
        var reply = await ErrorMapperHost.SendAsync(Invalid(), "Development");

        reply.Json.GetProperty("details").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ErrorReply_UsesCamelCaseNames()
    {
        var reply = await ErrorMapperHost.SendAsync(new NotFoundException("missing"));

        PropertyNames(reply.Json).Should().Equal("statusCode", "message", "traceId", "details");
        reply.Json.GetProperty("statusCode").GetInt32().Should().Be(404);
        reply.Json.GetProperty("message").GetString().Should().Be("missing");
        reply.Json.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidationReply_UsesCamelCaseNames()
    {
        var reply = await ErrorMapperHost.SendAsync(Invalid());

        PropertyNames(reply.Json).Should().Equal("statusCode", "message", "traceId", "errors", "details");
        var errors = reply.Json.GetProperty("errors").EnumerateArray().ToList();
        errors.Should().HaveCount(2);
        PropertyNames(errors[0]).Should().Equal("propertyName", "errorMessage");
        errors[0].GetProperty("propertyName").GetString().Should().Be("Name");
        errors[1].GetProperty("errorMessage").GetString().Should().Be("'Email' is not a valid email address.");
    }

    private static List<string> PropertyNames(JsonElement element)
        => element.EnumerateObject().Select(property => property.Name).ToList();

    private sealed class TeapotException() : HttpException((HttpStatusCode)418, "short and stout");

    private static Exception Create(string type) => type switch
    {
        nameof(ArgumentException) => new ArgumentException("bad argument"),
        nameof(BadRequestException) => new BadRequestException("bad request"),
        nameof(DomainException) => new DomainException("rule broken"),
        nameof(UnauthorizedException) => new UnauthorizedException("who are you"),
        nameof(ForbiddenException) => new ForbiddenException("not yours"),
        nameof(NotFoundException) => new NotFoundException("missing"),
        nameof(ConflictException) => new ConflictException("already exists"),
        nameof(InvalidOperationException) => new InvalidOperationException("internal state"),
        nameof(BadGatewayException) => new BadGatewayException("upstream failed"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static ValidationException Invalid() => new(
    [
        new ValidationFailure("Name", "'Name' must not be empty."),
        new ValidationFailure("Email", "'Email' is not a valid email address."),
    ]);
}
