using Blocks.Domain;

namespace Blocks.Entities;

public interface IAggregateRoot : IAggregateRoot<int>;

public abstract class AggregateRoot : AggregateRoot<int>, IAggregateRoot, IAuditedEntity;


public interface IAggregateRoot<TPrimaryKey> : IAuditedEntity<TPrimaryKey>
    where TPrimaryKey : struct
{
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }
    public void AddDomainEvent(IDomainEvent eventItem);
    public void ClearDomainEvents();
}

public abstract class AggregateRoot<TPrimaryKey> : Entity<TPrimaryKey>, IAggregateRoot<TPrimaryKey>
    where TPrimaryKey : struct
{
    // Only aggregates carry audit fields: other entities are saved as part of an aggregate and share its audit values.
    public TPrimaryKey CreatedById { get; init; }
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public TPrimaryKey? LastModifiedById { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    #region Domain Events
    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;
    public void AddDomainEvent(IDomainEvent eventItem) => _domainEvents.Add(eventItem);    
    public void ClearDomainEvents() => _domainEvents.Clear();
    #endregion
}