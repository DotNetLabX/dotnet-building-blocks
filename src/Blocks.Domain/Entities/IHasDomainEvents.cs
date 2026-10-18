using Blocks.Domain;

namespace Blocks.Entities;

public interface IHasDomainEvents
{
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }
    public void AddDomainEvent(IDomainEvent eventItem);
    public void ClearDomainEvents();
}
