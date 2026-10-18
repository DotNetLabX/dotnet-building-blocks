using System.Security.Claims;
using AwesomeAssertions;
using Blocks.Domain;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Blocks.AspNetCore.Tests;

public sealed class RenameNoteCommand : IAuditableAction
{
    public int CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Rename";
    public string? Comment => null;
}

public sealed class AssignUserIdFilterTests
{
    [Fact]
    public async Task IntCommand_APresetIdIsKept()
    {
        var command = new RenameNoteCommand { CreatedById = 5 };

        await RunFilterAsync(command, new Claim(ClaimTypes.NameIdentifier, "42"));

        command.CreatedById.Should().Be(5);
    }

    [Fact]
    public async Task IntCommand_AnUnsetIdIsFilledFromTheClaim()
    {
        var command = new RenameNoteCommand();

        await RunFilterAsync(command, new Claim(ClaimTypes.NameIdentifier, "42"));

        command.CreatedById.Should().Be(42);
    }

    [Fact]
    public async Task IntCommand_AnUnsetIdStaysUnsetWithoutAClaim()
    {
        var command = new RenameNoteCommand();

        await RunFilterAsync(command);

        command.CreatedById.Should().Be(0);
    }

    private static async Task RunFilterAsync(object argument, params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        var filter = new AssignUserIdFilter(new HttpContextProvider(new HttpContextAccessor { HttpContext = httpContext }));
        var invocation = EndpointFilterInvocationContext.Create(httpContext, argument);

        await filter.InvokeAsync(invocation, _ => ValueTask.FromResult<object?>(null));
    }
}

public sealed class ShareNoteCommand : IAuditableActionBy<string>
{
    public string? CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Share";
    public string? Comment => null;
}

public sealed class AssignUserIdFilterWhoTypedTests
{
    [Fact]
    public async Task WhoTypedCommand_APresetIdIsKept()
    {
        var command = new ShareNoteCommand { CreatedById = "auth0|preset" };

        await RunFilterAsync(command, new Claim(ClaimTypes.NameIdentifier, "auth0|caller"));

        command.CreatedById.Should().Be("auth0|preset");
    }

    [Fact]
    public async Task WhoTypedCommand_AnUnsetIdIsFilledWithTheClaimText()
    {
        var command = new ShareNoteCommand();

        await RunFilterAsync(command, new Claim(ClaimTypes.NameIdentifier, "auth0|caller"));

        command.CreatedById.Should().Be("auth0|caller");
    }

    [Fact]
    public async Task WhoTypedCommand_AnUnsetIdStaysUnsetWithoutAClaim()
    {
        var command = new ShareNoteCommand();

        await RunFilterAsync(command);

        command.CreatedById.Should().BeNull();
    }

    private static async Task RunFilterAsync(object argument, params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        var filter = new AssignUserIdFilter(new HttpContextProvider(new HttpContextAccessor { HttpContext = httpContext }));
        var invocation = EndpointFilterInvocationContext.Create(httpContext, argument);

        await filter.InvokeAsync(invocation, _ => ValueTask.FromResult<object?>(null));
    }
}
