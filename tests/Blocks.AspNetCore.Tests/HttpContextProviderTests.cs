using System.Security.Claims;
using AwesomeAssertions;
using Blocks.Core.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Blocks.AspNetCore.Tests;

public sealed class HttpContextProviderTests
{
    private const string ScopeClaim = "scope";

    [Fact]
    public void GetClaimValues_ReturnsTheValuesOfTheClaimAskedFor()
    {
        var provider = ProviderFor(
            new Claim(ClaimTypes.Role, "editor"),
            new Claim(ClaimTypes.Role, "reviewer"),
            new Claim(ScopeClaim, "notes.read"),
            new Claim(ScopeClaim, "notes.write"));

        provider.GetClaimValues(ClaimTypes.Role).Should().BeEquivalentTo("editor", "reviewer");
        provider.GetClaimValues(ScopeClaim).Should().BeEquivalentTo("notes.read", "notes.write");
    }

    [Fact]
    public void GetClaimValues_ReturnsNothingForAClaimThePrincipalLacks()
    {
        var provider = ProviderFor(new Claim(ClaimTypes.Role, "editor"));

        provider.GetClaimValues(ScopeClaim).Should().BeEmpty();
    }

    private static HttpContextProvider ProviderFor(params Claim[] claims)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        return new HttpContextProvider(new HttpContextAccessor { HttpContext = context });
    }
}

internal sealed class NumberOnlyClaimsProvider(int? userId) : IClaimsProvider
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

public sealed class ClaimsProviderUserIdTextTests
{
    [Fact]
    public void HttpContextProvider_ReturnsATextUserIdClaimUnchanged()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "auth0|caller")], "test"))
        };
        IClaimsProvider provider = new HttpContextProvider(new HttpContextAccessor { HttpContext = context });

        provider.TryGetUserIdText().Should().Be("auth0|caller");
        provider.GetUserIdText().Should().Be("auth0|caller");
    }

    [Fact]
    public void ClaimsProviderWithTodaysMembersOnly_GivesTheNumberAsText()
    {
        IClaimsProvider provider = new NumberOnlyClaimsProvider(42);

        provider.TryGetUserIdText().Should().Be("42");
        provider.GetUserIdText().Should().Be("42");
    }

    [Fact]
    public void ClaimsProviderWithTodaysMembersOnly_ThrowsForAMissingUserIdText()
    {
        IClaimsProvider provider = new NumberOnlyClaimsProvider(null);

        provider.TryGetUserIdText().Should().BeNull();
        provider.Invoking(p => p.GetUserIdText()).Should().Throw<UnauthorizedAccessException>();
    }
}
