using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Blocks.Core.Security;
using Blocks.Domain;
using Blocks.MediatR.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blocks.MediatR.Tests;

public sealed class RenameNote : IRequest<int>, IAuditableAction
{
    public int CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Rename";
    public string? Comment => null;
}

public sealed class RenameNoteHandler : IRequestHandler<RenameNote, int>
{
    public Task<int> Handle(RenameNote request, CancellationToken cancellationToken) => Task.FromResult(request.CreatedById);
}

public sealed record Ping : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult("pong");
}

internal sealed class NumberClaimsProvider(int? userId) : IClaimsProvider
{
    public string GetClaimValue(string claimName) => throw new InvalidOperationException(claimName);
    public string? TryGetClaimValue(string claimName) => null;
    public IEnumerable<string> GetClaimValues(string claimName) => [];
    public int GetUserId() => userId ?? throw new UnauthorizedAccessException();
    public int? TryGetUserId() => userId;
    public string GetUserEmail() => throw new InvalidOperationException();
    public string GetUserName() => throw new InvalidOperationException();
    public IReadOnlySet<TEnum> GetUserRoles<TEnum>() where TEnum : struct, Enum => new HashSet<TEnum>();
    public IReadOnlySet<string> GetUserRoles() => new HashSet<string>();
}

public sealed class AssignUserIdBehaviorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task IntCommand_ThePresetIdIsReplacedByTheClaim()
    {
        await using var provider = BuildProvider(new NumberClaimsProvider(42));

        var createdById = await provider.GetRequiredService<IMediator>().Send(new RenameNote { CreatedById = 5 }, Ct);

        createdById.Should().Be(42);
    }

    [Fact]
    public async Task IntCommand_ThePresetIdIsKeptWhenNoClaimIsPresent()
    {
        await using var provider = BuildProvider(new NumberClaimsProvider(null));

        var createdById = await provider.GetRequiredService<IMediator>().Send(new RenameNote { CreatedById = 5 }, Ct);

        createdById.Should().Be(5);
    }

    [Fact]
    public async Task RequestThatIsNotAuditable_IsHandledWithNoClaimsReaderRegistered()
    {
        await using var provider = BuildProvider(claimsProvider: null);

        var reply = await provider.GetRequiredService<IMediator>().Send(new Ping(), Ct);

        reply.Should().Be("pong");
    }

    private static ServiceProvider BuildProvider(IClaimsProvider? claimsProvider)
    {
        var services = new ServiceCollection();
        if (claimsProvider is not null)
            services.AddSingleton(claimsProvider);
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<AssignUserIdBehaviorTests>();
            config.AddOpenBehavior(typeof(AssignUserIdBehavior<,>));
        });
        return services.BuildServiceProvider();
    }
}

public enum NoteAction
{
    Shared
}

public sealed class ShareNote : IRequest<string?>, IAuditableActionBy<string, NoteAction>
{
    public string? CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public NoteAction ActionType => NoteAction.Shared;
    public string? Comment => null;
}

public sealed class ShareNoteHandler : IRequestHandler<ShareNote, string?>
{
    public Task<string?> Handle(ShareNote request, CancellationToken cancellationToken) => Task.FromResult(request.CreatedById);
}

internal sealed class TextClaimsProvider(string? userId) : IClaimsProvider
{
    public string GetClaimValue(string claimName) => throw new InvalidOperationException(claimName);
    public string? TryGetClaimValue(string claimName) => null;
    public IEnumerable<string> GetClaimValues(string claimName) => [];
    public int GetUserId() => throw new InvalidOperationException();
    public int? TryGetUserId() => throw new InvalidOperationException();
    public string? TryGetUserIdText() => userId;
    public string GetUserEmail() => throw new InvalidOperationException();
    public string GetUserName() => throw new InvalidOperationException();
    public IReadOnlySet<TEnum> GetUserRoles<TEnum>() where TEnum : struct, Enum => new HashSet<TEnum>();
    public IReadOnlySet<string> GetUserRoles() => new HashSet<string>();
}

public sealed class AssignUserIdBehaviorWhoTypedTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WhoTypedCommand_AForgedPresetIdIsReplacedByTheClaimText()
    {
        await using var provider = BuildProvider(new TextClaimsProvider("auth0|caller"));

        var createdById = await provider.GetRequiredService<IMediator>().Send(new ShareNote { CreatedById = "auth0|forged" }, Ct);

        createdById.Should().Be("auth0|caller");
    }

    [Fact]
    public async Task WhoTypedCommand_ThePresetIdIsKeptWhenNoClaimIsPresent()
    {
        await using var provider = BuildProvider(new TextClaimsProvider(null));

        var createdById = await provider.GetRequiredService<IMediator>().Send(new ShareNote { CreatedById = "auth0|preset" }, Ct);

        createdById.Should().Be("auth0|preset");
    }

    [Fact]
    public void WhoTypedCommand_TakesItsActionFromTheActionType()
    {
        IAuditableActionBy<string> command = new ShareNote();

        command.Action.Should().Be(nameof(NoteAction.Shared));
        command.IsAuthenticated.Should().BeFalse();
    }

    private static ServiceProvider BuildProvider(IClaimsProvider claimsProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(claimsProvider);
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<AssignUserIdBehaviorWhoTypedTests>();
            config.AddOpenBehavior(typeof(AssignUserIdBehavior<,>));
        });
        return services.BuildServiceProvider();
    }
}

public sealed record UserKey(string Value) : IParsable<UserKey>
{
    public static UserKey Parse(string s, IFormatProvider? provider) => new(s);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out UserKey result)
    {
        result = s is null ? null : new UserKey(s);
        return result is not null;
    }
}

public sealed class DualWhoCommand : IAuditableActionBy<string>, IAuditableActionBy<UserKey>
{
    public string? CreatedById { get; set; }
    public UserKey? KeyCreatedById { get; set; }
    UserKey? IAuditableActionBy<UserKey>.CreatedById { get => KeyCreatedById; set => KeyCreatedById = value; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Dual";
    public string? Comment => null;
}

public sealed class AuditableActionExtensionsTests
{
    [Fact]
    public void TryAssignCreatedBy_SetsTheIdOfAWhoTypedRequest()
    {
        var command = new ShareNote();

        command.TryAssignCreatedBy("auth0|caller", keepExisting: false).Should().BeTrue();

        command.CreatedById.Should().Be("auth0|caller");
    }

    [Fact]
    public void TryAssignCreatedBy_KeepsASetIdWhenAskedTo()
    {
        var command = new ShareNote { CreatedById = "auth0|preset" };

        command.TryAssignCreatedBy("auth0|caller", keepExisting: true).Should().BeFalse();

        command.CreatedById.Should().Be("auth0|preset");
    }

    [Fact]
    public void TryAssignCreatedBy_FillsAnUnsetIdWhenAskedToKeepASetOne()
    {
        var command = new ShareNote();

        command.TryAssignCreatedBy("auth0|caller", keepExisting: true).Should().BeTrue();

        command.CreatedById.Should().Be("auth0|caller");
    }

    [Fact]
    public void TryAssignCreatedBy_FillsEveryUnsetWhoTypeOfARequestCarryingTwo()
    {
        var textSet = new DualWhoCommand { CreatedById = "auth0|preset" };
        var keySet = new DualWhoCommand { KeyCreatedById = new UserKey("preset") };

        textSet.TryAssignCreatedBy("auth0|caller", keepExisting: true).Should().BeTrue();
        keySet.TryAssignCreatedBy("auth0|caller", keepExisting: true).Should().BeTrue();

        textSet.CreatedById.Should().Be("auth0|preset");
        textSet.KeyCreatedById.Should().Be(new UserKey("auth0|caller"));
        keySet.CreatedById.Should().Be("auth0|caller");
        keySet.KeyCreatedById.Should().Be(new UserKey("preset"));
    }

    [Fact]
    public void TryAssignCreatedBy_LeavesAnIntCommandAndAnyOtherObjectAlone()
    {
        var command = new RenameNote { CreatedById = 5 };

        command.TryAssignCreatedBy("42", keepExisting: false).Should().BeFalse();
        new Ping().TryAssignCreatedBy("42", keepExisting: false).Should().BeFalse();

        command.CreatedById.Should().Be(5);
    }
}
