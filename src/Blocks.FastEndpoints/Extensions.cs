using Blocks.AspNetCore;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;

namespace Blocks.FastEndpoints;

public static class Extensions
{
    private const string MultipleFieldsMessage = "Validation failed for multiple fields.";

    public static IApplicationBuilder UseCustomFastEndpoints(this IApplicationBuilder app, Action<Config>? configure = null)
    {
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";
            c.Serializer.Options.PropertyNameCaseInsensitive = true;
            c.UseEnumsAsText();
            c.UseFieldMapValidationReply();
            configure?.Invoke(c);
        });

        return app;
    }

    public static void UseEnumsAsText(this Config config)
        => config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());

    public static void UseFieldMapValidationReply(this Config config)
    {
        config.Errors.ResponseBuilder = (failures, _, statusCode) => new FieldMapValidationReply(
            statusCode,
            failures.Count == 1 ? failures[0].ErrorMessage : MultipleFieldsMessage,
            failures.ToFieldMap(config.Serializer.Options.PropertyNamingPolicy));
        config.Errors.ProducesMetadataType = typeof(FieldMapValidationReply);
    }
}
