using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Blocks.Domain;

public static class AuditableActionExtensions
{
    private static readonly MethodInfo AssignMethod =
        typeof(AuditableActionExtensions).GetMethod(nameof(Assign), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<object, string, bool, bool>[]> Assigners = new();

    public static bool TryAssignCreatedBy(this object request, string userId, bool keepExisting)
    {
        var assigned = false;
        foreach (var assign in Assigners.GetOrAdd(request.GetType(), BuildAssigners))
            assigned |= assign(request, userId, keepExisting);

        return assigned;
    }

    private static Func<object, string, bool, bool>[] BuildAssigners(Type requestType)
        => requestType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuditableActionBy<>))
            .Select(i => AssignMethod.MakeGenericMethod(i.GetGenericArguments()[0]).CreateDelegate<Func<object, string, bool, bool>>())
            .ToArray();

    private static bool Assign<TUserId>(object request, string userId, bool keepExisting)
        where TUserId : class, IParsable<TUserId>
    {
        var action = (IAuditableActionBy<TUserId>)request;
        if (keepExisting && action.CreatedById is not null)
            return false;

        action.CreatedById = TUserId.Parse(userId, CultureInfo.InvariantCulture);
        return true;
    }
}
