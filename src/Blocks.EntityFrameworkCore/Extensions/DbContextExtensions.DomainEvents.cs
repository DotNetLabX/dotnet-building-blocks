using Blocks.Core;
using Blocks.Domain;

namespace Blocks.EntityFrameworkCore;

public static partial class DbContextExtensions
{
    public static async Task<int> DispatchDomainEventsAsync(this DbContext ctx, IDomainEventPublisher eventPublisher, CancellationToken ct = default)
    {
        var aggregates = ctx.ChangeTracker
            .Entries()
            .Select(a => a.Entity)
            .OfType<IAggregateRoot>()
            .Where(a => a.DomainEvents.Any())
            .ToList();
        
        if(aggregates.IsEmpty())
            return 0;

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregates.ToList()
            .ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await eventPublisher.PublishAsync(domainEvent, ct);

        return domainEvents.Count;
    }
}
