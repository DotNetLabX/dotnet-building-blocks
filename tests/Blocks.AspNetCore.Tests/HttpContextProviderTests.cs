using System.Security.Claims;
using AwesomeAssertions;
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
