using Blocks.Core.Security;
using Blocks.Domain;
using Microsoft.AspNetCore.Http;

namespace Blocks.AspNetCore;

public sealed class AssignUserIdFilter(IClaimsProvider _claimsProvider) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        foreach (var arg in ctx.Arguments)
        {
            if (arg is IAuditableAction action && action.CreatedById == default)
            {
                var userId = _claimsProvider.TryGetUserId();
                if (userId is not null)
                    action.CreatedById = userId.Value;
            }
            else if (arg is IAuditableRequest request and not IAuditableAction)
            {
                var userId = _claimsProvider.TryGetUserIdText();
                if (userId is not null)
                    request.TryAssignCreatedBy(userId, keepExisting: true);
            }
        }
        return await next(ctx);
    }
}
