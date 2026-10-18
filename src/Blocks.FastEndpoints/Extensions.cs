using Blocks.AspNetCore;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using System.Text.Json.Serialization;

namespace Blocks.FastEndpoints;

public static class Extensions
{
    private const string MultipleFieldsMessage = "Validation failed for multiple fields.";

    public static IApplicationBuilder UseCustomFastEndpoints(this IApplicationBuilder app)
    {
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";
            c.UseEnumsAsText();
            c.UseFieldMapValidationReply();
        });

        return app;
    }

    public static void UseEnumsAsText(this Config config)
        => config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());

    public static void UseFieldMapValidationReply(this Config config)
        => config.Errors.ResponseBuilder = (failures, _, statusCode) => new
        {
            statusCode,
            message = failures.Count == 1 ? failures[0].ErrorMessage : MultipleFieldsMessage,
            errors = failures.ToFieldMap(config.Serializer.Options.PropertyNamingPolicy)
        };
}
