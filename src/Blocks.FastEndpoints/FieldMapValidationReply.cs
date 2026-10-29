using System.Text.Json.Serialization;

namespace Blocks.FastEndpoints;

public sealed record FieldMapValidationReply(
    [property: JsonPropertyName("statusCode")] int StatusCode,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("errors")] Dictionary<string, string[]> Errors);
