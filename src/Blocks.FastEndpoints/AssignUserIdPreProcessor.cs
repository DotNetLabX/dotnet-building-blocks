using System.Collections.Concurrent;
using Blocks.Core.Security;
using Blocks.Domain;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Blocks.FastEndpoints;

public class AssignUserIdPreProcessor : IGlobalPreProcessor
{
    private static readonly ConcurrentDictionary<Type, bool> WhoTypedRequests = new();

    public Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        if (context.Request is IAuditableAction command)
        {
            var claimsProvider = context.HttpContext.RequestServices.GetRequiredService<IClaimsProvider>();
            command.CreatedById = claimsProvider.GetUserId();
        }
        else if (context.Request is IAuditableRequest request && IsWhoTyped(request.GetType()))
        {
            var claimsProvider = context.HttpContext.RequestServices.GetRequiredService<IClaimsProvider>();
            request.TryAssignCreatedBy(claimsProvider.GetUserIdText(), keepExisting: false);
        }

        return Task.CompletedTask;
    }

    private static bool IsWhoTyped(Type requestType)
        => WhoTypedRequests.GetOrAdd(requestType, type => type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuditableActionBy<>)));
}
