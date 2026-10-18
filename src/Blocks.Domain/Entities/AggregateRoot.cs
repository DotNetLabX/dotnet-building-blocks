using Blocks.Domain;

namespace Blocks.Entities;

public interface IAggregateRoot : IAggregateRoot<int>;

public abstract class AggregateRoot : AggregateRoot<int>, IAggregateRoot, IAuditedEntity;


public interface IAggregateRoot<TPrimaryKey> : IAuditedEntity<TPrimaryKey>, IHasDomainEvents
    where TPrimaryKey : struct
{
    public new IReadOnlyList<IDomainEvent> DomainEvents { get; }
    public new void AddDomainEvent(IDomainEvent eventItem);
    public new void ClearDomainEvents();

    IReadOnlyList<IDomainEvent> IHasDomainEvents.DomainEvents => DomainEvents;
    void IHasDomainEvents.AddDomainEvent(IDomainEvent eventItem) => AddDomainEvent(eventItem);
    void IHasDomainEvents.ClearDomainEvents() => ClearDomainEvents();
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

public interface IAggregateRoot<TPrimaryKey, TUserId> : IAuditedEntity<TPrimaryKey, TUserId>, IHasDomainEvents
    where TPrimaryKey : struct
    where TUserId : class;

public abstract class AggregateRoot<TPrimaryKey, TUserId> : Entity<TPrimaryKey>, IAggregateRoot<TPrimaryKey, TUserId>
    where TPrimaryKey : struct
    where TUserId : class
{
    public TUserId CreatedById { get; init; } = default!;
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public TUserId? LastModifiedById { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    #region Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;
    public void AddDomainEvent(IDomainEvent eventItem) => _domainEvents.Add(eventItem);
    public void ClearDomainEvents() => _domainEvents.Clear();
    #endregion
}
