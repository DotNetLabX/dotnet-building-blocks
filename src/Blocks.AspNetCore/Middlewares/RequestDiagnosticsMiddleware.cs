using Blocks.Core.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;

namespace Blocks.AspNetCore.Middlewares;

public sealed class RequestDiagnosticsMiddleware(
    RequestDelegate _next,
    ILogger<RequestDiagnosticsMiddleware> _logger,
    IWebHostEnvironment _env,
    IOptions<RequestDiagnosticsOptions> options)
{
    private readonly RequestDiagnosticsOptions _limits = options.Value;

    public async Task InvokeAsync(HttpContext ctx, RequestContext requestContext)
    {
        var correlationId = ctx.Items["X-Correlation-ID"] as string ?? ctx.TraceIdentifier;

        _logger.LogDebug("[Begin] HTTP {Method} {Path} | CorrelationId: {CorrelationId}",
            ctx.Request.Method, ctx.Request.Path, correlationId);

        var sw = Stopwatch.StartNew();

        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey("X-Correlation-ID"))
                ctx.Response.Headers["X-Correlation-ID"] = correlationId;

            if (_env.IsDevelopment() && !ctx.Response.Headers.ContainsKey("X-Elapsed-Ms"))
                ctx.Response.Headers.Append("X-Elapsed-Ms", sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));

            return Task.CompletedTask;
        });

        try 
        { 
            await _next(ctx); 
        }
        finally
        {
            sw.Stop();

            var elapsedMs = sw.ElapsedMilliseconds;

            var thresholdMs = requestContext.IsFileTransfer ? _limits.SlowFileTransferMs : _limits.SlowRequestMs;
            if (elapsedMs > thresholdMs && !IsStreamed(ctx.Response))
            {
                _logger.LogWarning("[PerfWarn] {Method} {Path} took {Elapsed} ms. (FileTransfer: {IsFileTransfer})",
                    ctx.Request.Method, ctx.Request.Path, elapsedMs, requestContext.IsFileTransfer);
            }

            _logger.LogDebug("[End] HTTP {Method} {Path} -> {Status} in {Elapsed}ms | CorrelationId: {CorrelationId}",
                ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, elapsedMs, correlationId);
        }
    }

    private static bool IsStreamed(HttpResponse response)
        => response.ContentType?.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase) ?? false;
}
