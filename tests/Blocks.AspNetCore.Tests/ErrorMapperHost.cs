using System.Net;
using System.Text.Json;
using Blocks.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Blocks.AspNetCore.Tests;

internal sealed record ErrorReply(HttpStatusCode Status, string Body)
{
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;
}

internal static class ErrorMapperHost
{
    public static Task<ErrorReply> SendAsync(Exception thrown, string environment = "Production", CapturedLogs? logs = null, bool clientAborted = false)
        => SendAsync(_ => throw thrown, environment, logs, clientAborted);

    public static async Task<ErrorReply> SendAsync(
        RequestDelegate endpoint, string environment = "Production", CapturedLogs? logs = null, bool clientAborted = false)
    {
        using var host = await new HostBuilder()
            .ConfigureLogging(logging =>
            {
                if (logs is not null)
                    logging.ClearProviders().AddProvider(logs);
            })
            .ConfigureWebHost(web => web
                .UseTestServer()
                .UseEnvironment(environment)
                .Configure(app =>
                {
                    if (clientAborted)
                    {
                        app.Use((context, next) =>
                        {
                            context.Features.Set<IHttpRequestLifetimeFeature>(new AbortedRequest());
                            return next(context);
                        });
                    }

                    app.UseMiddleware<GlobalExceptionMiddleware>();
                    app.Run(endpoint);
                }))
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return new ErrorReply(response.StatusCode, body);
    }
}

internal sealed class AbortedRequest : IHttpRequestLifetimeFeature
{
    public CancellationToken RequestAborted { get; set; } = new(canceled: true);

    public void Abort()
    {
    }
}

internal sealed record CapturedLog(string Category, LogLevel Level, Exception? Exception);

internal sealed class CapturedLogs : ILoggerProvider
{
    private readonly List<CapturedLog> _entries = [];

    public IReadOnlyList<CapturedLog> Entries
    {
        get
        {
            lock (_entries)
                return [.. _entries];
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Logger(CapturedLogs owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (owner._entries)
                owner._entries.Add(new CapturedLog(category, logLevel, exception));
        }
    }
}
