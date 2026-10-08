using Blocks.Core.Security;
using Blocks.Domain;
using FastEndpoints;

namespace Blocks.FastEndpoints;

public class AssignUserIdPreProcessor : IGlobalPreProcessor
{
    public Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        if (context.Request is IAuditableAction command)
        {
            var claimsProvider = context.HttpContext.Resolve<IClaimsProvider>();
            command.CreatedById = claimsProvider.GetUserId();
        }

        return Task.CompletedTask;
    }
}
