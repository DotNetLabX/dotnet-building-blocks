using System.Security.Claims;
using AwesomeAssertions;
using Blocks.AspNetCore;
using Blocks.Core.Security;
using Blocks.Domain;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blocks.FastEndpoints.Tests;

public sealed class ArchiveNoteCommand : IAuditableAction
{
    public int CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Archive";
    public string? Comment => null;
}

internal sealed class FakePreProcessorContext(object request, HttpContext httpContext) : IPreProcessorContext
{
    public object? Request => request;
    public HttpContext HttpContext => httpContext;
    public List<ValidationFailure> ValidationFailures { get; } = [];
    public bool HasValidationFailures => ValidationFailures.Count > 0;
}

public sealed class AssignUserIdPreProcessorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task IntCommand_APresetIdIsReplacedByTheClaim()
    {
        var command = new ArchiveNoteCommand { CreatedById = 5 };

        await RunAsync(command, new Claim(ClaimTypes.NameIdentifier, "42"));

        command.CreatedById.Should().Be(42);
    }

    [Fact]
    public async Task IntCommand_AMissingClaimThrows()
    {
        var command = new ArchiveNoteCommand { CreatedById = 5 };

        var act = () => RunAsync(command);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static Task RunAsync(object request, params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IClaimsProvider>(new HttpContextProvider(new HttpContextAccessor { HttpContext = httpContext }))
            .BuildServiceProvider();

        return new AssignUserIdPreProcessor().PreProcessAsync(new FakePreProcessorContext(request, httpContext), Ct);
    }
}

public sealed class PublishNoteCommand : IAuditableActionBy<string>
{
    public string? CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Publish";
    public string? Comment => null;
}

public sealed record MarkedOnlyRequest : IAuditableRequest;

public sealed class AssignUserIdPreProcessorWhoTypedTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WhoTypedCommand_AForgedPresetIdIsReplacedByTheClaimText()
    {
        var command = new PublishNoteCommand { CreatedById = "auth0|forged" };

        await RunAsync(command, new Claim(ClaimTypes.NameIdentifier, "auth0|caller"));

        command.CreatedById.Should().Be("auth0|caller");
    }

    [Fact]
    public async Task WhoTypedCommand_AMissingClaimThrows()
    {
        var command = new PublishNoteCommand { CreatedById = "auth0|forged" };

        var act = () => RunAsync(command);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RequestCarryingOnlyTheMarker_IsLeftAloneWithoutAClaim()
    {
        var act = () => RunAsync(new MarkedOnlyRequest());

        await act.Should().NotThrowAsync();
    }

    private static Task RunAsync(object request, params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IClaimsProvider>(new HttpContextProvider(new HttpContextAccessor { HttpContext = httpContext }))
            .BuildServiceProvider();

        return new AssignUserIdPreProcessor().PreProcessAsync(new FakePreProcessorContext(request, httpContext), Ct);
    }
}
