using MediatR;
using Blocks.Core.Security;
using Blocks.Domain;

namespace Blocks.MediatR.Behaviors;

public class AssignUserIdBehavior<TRequest, TResponse>
    (IClaimsProvider _claimsProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditableRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IAuditableAction action)
        {
            var userId = _claimsProvider.TryGetUserId();
            if (userId is not null)
                action.CreatedById = userId.Value;
        }
        else
        {
            var userId = _claimsProvider.TryGetUserIdText();
            if (userId is not null)
                request.TryAssignCreatedBy(userId, keepExisting: false);
        }

        return await next();
    }
}
