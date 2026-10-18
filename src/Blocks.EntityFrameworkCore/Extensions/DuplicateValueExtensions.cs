using System.Reflection;
using Blocks.Exceptions;

namespace Blocks.EntityFrameworkCore;

internal static class DuplicateValueExtensions
{
    private const string ConflictMessage = "A record with the same unique value already exists.";

    public static bool IsDuplicateValue(this DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (IsDuplicateValueError(inner))
                return true;
        }
        return false;
    }

    public static ConflictException ToConflict(this DbUpdateException exception) => new(ConflictMessage, exception);

    private static bool IsDuplicateValueError(Exception error) => error.GetType().FullName switch
    {
        "Microsoft.Data.SqlClient.SqlException" or "System.Data.SqlClient.SqlException"
            => ReadProperty(error, "Number") is 2601 or 2627,
        "Npgsql.PostgresException" => ReadProperty(error, "SqlState") is "23505",
        _ => false
    };

    private static object? ReadProperty(Exception error, string name)
        => error.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(error);
}
