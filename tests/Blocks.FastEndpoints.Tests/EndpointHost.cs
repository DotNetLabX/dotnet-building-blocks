using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

[assembly: AssemblyFixture(typeof(Blocks.FastEndpoints.Tests.EndpointHost))]

namespace Blocks.FastEndpoints.Tests;

// The endpoint library keeps its set-up in static fields, so this test process starts exactly one endpoint host.
public sealed class EndpointHost : IAsyncLifetime
{
    private WebApplication? _app;

    public HttpClient CreateClient() => (_app ?? throw new InvalidOperationException("The endpoint host has not started.")).GetTestClient();

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options => options.Assemblies = [typeof(EndpointHost).Assembly]);

        var app = builder.Build();
        app.UseCustomFastEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        _app = app;
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is null)
            return;

        await _app.StopAsync(CancellationToken.None);
        await _app.DisposeAsync();
    }
}
