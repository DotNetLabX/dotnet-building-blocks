using Blocks.Core.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Blocks.AspNetCore.Middlewares;

public sealed class RequestContextMiddleware(RequestDelegate _next, ILogger<RequestContextMiddleware> _log)
{
    private const string CorrelationIDHeader = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext httpContext, RequestContext requestContext)
    {
        var correlationId = ResolveCorrelationId(httpContext);    

        httpContext.Items[CorrelationIDHeader] = correlationId;
        requestContext.CorrelationId = correlationId;

        requestContext.IsUpload = (httpContext.Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ?? false);
        requestContext.IsDownload = httpContext.Request.Path.Value?.IndexOf("download", StringComparison.OrdinalIgnoreCase) >= 0;
        requestContext.RemoteIp = httpContext.Connection.RemoteIpAddress?.ToString();

        httpContext.Response.OnStarting(() =>
        {
            if (!httpContext.Response.Headers.ContainsKey(CorrelationIDHeader))
                httpContext.Response.Headers[CorrelationIDHeader] = requestContext.CorrelationId ?? string.Empty;
            return Task.CompletedTask;
        });

        using (_log.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = requestContext.CorrelationId ?? "" }))
        {
            await _next(httpContext);
        }
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationIDHeader, out var values) && values.Count == 1 && IsPlain(values[0]))
            return values[0]!;

        var activityId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(activityId))
            return activityId!;

        return httpContext.TraceIdentifier;
    }

    private static bool IsPlain(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxCorrelationIdLength && value.All(IsAllowed);

    private static bool IsAllowed(char c) =>
        char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':';
}
