using System.Text.Json;
using FluentValidation.Results;

namespace Blocks.AspNetCore;

public static class ValidationFailureExtensions
{
    public static Dictionary<string, string[]> ToFieldMap(this IEnumerable<ValidationFailure> failures, JsonNamingPolicy? namingPolicy)
        => failures
            .GroupBy(failure => FieldKey(failure.PropertyName ?? "", namingPolicy), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray(), StringComparer.Ordinal);

    private static string FieldKey(string propertyPath, JsonNamingPolicy? namingPolicy)
        => namingPolicy is null
            ? propertyPath
            : string.Join('.', propertyPath.Split('.').Select(part => ConvertPart(part, namingPolicy)));

    private static string ConvertPart(string part, JsonNamingPolicy namingPolicy)
    {
        var indexerStart = part.IndexOf('[', StringComparison.Ordinal);
        var name = indexerStart < 0 ? part : part[..indexerStart];
        return name.Length == 0 ? part : namingPolicy.ConvertName(name) + part[name.Length..];
    }
}
