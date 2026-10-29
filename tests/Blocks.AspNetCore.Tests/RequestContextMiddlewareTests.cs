using System.Text.RegularExpressions;
using AwesomeAssertions;
using Blocks.AspNetCore.Middlewares;
using Blocks.Core.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Blocks.AspNetCore.Tests;

public sealed partial class RequestContextMiddlewareTests
{
    private const string Header = "X-Correlation-ID";

    private static readonly string Longest = new('a', 128);

    [Theory]
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    [InlineData("order_42.retry:1")]
    public async Task APlainId_IsKept_OnTheReplyAndInTheContext(string sent)
    {
        var (header, context) = await SendAsync(sent);

        header.Should().Be(sent);
        context.Should().Be(sent);
    }

    [Fact]
    public async Task AnIdOfExactlyTheLimit_IsKept()
    {
        var (header, context) = await SendAsync(Longest);

        header.Should().Be(Longest);
        context.Should().Be(Longest);
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public async Task AnIdTooLongOrNotPlain_IsReplaced(string sent)
    {
        var (header, context) = await SendAsync(sent);

        header.Should().NotBe(sent);
        context.Should().Be(header);
        header.Should().MatchRegex(AllowedShape());
    }

    [Fact]
    public async Task AnIdSentTwice_IsReplaced()
    {
        var (header, context) = await SendAsync("first", "second");

        new[] { "first", "second", "first,second", "first, second" }.Should().NotContain(header);
        context.Should().Be(header);
        header.Should().MatchRegex(AllowedShape());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnAbsentOrBlankId_FallsBack(string? sent)
    {
        var (header, context) = sent is null ? await SendAsync() : await SendAsync(sent);

        header.Should().NotBeNullOrWhiteSpace();
        context.Should().Be(header);
        header.Should().MatchRegex(AllowedShape());
    }

    public static TheoryData<string> Rejected() =>
    [
        Longest + "a",
        "has space",
        "has<angle",
        "café",
    ];

    [GeneratedRegex("^[A-Za-z0-9_.:-]{1,128}$")]
    private static partial Regex AllowedShape();

    private static async Task<(string Header, string Context)> SendAsync(params string[] sent)
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddScoped<RequestContext>())
                .Configure(app =>
                {
                    app.UseMiddleware<RequestContextMiddleware>();
                    app.Run(context => context.Response.WriteAsync(
                        context.RequestServices.GetRequiredService<RequestContext>().CorrelationId ?? string.Empty));
                }))
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (sent.Length > 0)
            request.Headers.TryAddWithoutValidation(Header, sent).Should().BeTrue();

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (string.Join(",", response.Headers.GetValues(Header)), body);
    }
}
