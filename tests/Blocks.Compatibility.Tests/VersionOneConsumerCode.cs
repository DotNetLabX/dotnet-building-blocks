using Blocks.Core.Security;
using Blocks.Domain;
using Blocks.Entities;
using Blocks.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Caching.Memory;

namespace Blocks.Compatibility.Tests;

public sealed class Order : AggregateRoot
{
    public string Reference { get; set; } = string.Empty;
}

public sealed class Shipment : AggregateRoot<Guid>
{
    public string Carrier { get; set; } = string.Empty;
}

public sealed class OrderConfiguration : AuditedEntityConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);
        builder.Ignore(e => e.DomainEvents);
    }
}

public sealed class ShipmentConfiguration : AuditedEntityConfiguration<Shipment, Guid>
{
    public override void Configure(EntityTypeBuilder<Shipment> builder)
    {
        base.Configure(builder);
        builder.Ignore(e => e.DomainEvents);
    }
}

public sealed class AuditReset
{
    public void ClearLastModifiedBy(Order order, Shipment shipment)
    {
        order.LastModifiedById = null;
        shipment.LastModifiedById = null;
    }
}

public sealed class FixedClaimsProvider : IClaimsProvider
{
    public const int UserId = 7;

    public string GetClaimValue(string claimName)
        => TryGetClaimValue(claimName) ?? throw new InvalidOperationException($"Missing claim: {claimName}");
    public string? TryGetClaimValue(string claimName) => null;
    public IEnumerable<string> GetClaimValues(string claimName) => [];

    public int GetUserId() => UserId;
    public int? TryGetUserId() => UserId;
    public string GetUserEmail() => "user@example.test";
    public string GetUserName() => "user";

    public IReadOnlySet<TEnum> GetUserRoles<TEnum>()
        where TEnum : struct, Enum
        => new HashSet<TEnum>();

    public IReadOnlySet<string> GetUserRoles() => new HashSet<string>();
}

public enum OrderAction
{
    Placed,
    Cancelled
}

public sealed class PlaceOrderCommand : IAuditableAction<OrderAction>
{
    public int CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public OrderAction ActionType => OrderAction.Placed;
    public string? Comment { get; init; }
}

public sealed class RenameOrderCommand : IAuditableAction
{
    public int CreatedById { get; set; }
    public DateTime CreatedOn { get; } = DateTime.UtcNow;
    public string Action => "Rename";
    public string? Comment { get; init; }
}

public sealed class OrderContext(DbContextOptions<OrderContext> options, IMemoryCache cache)
    : ApplicationDbContext<OrderContext>(options, cache);

public sealed class OrderContextFactory : DesignTimeFactoryBase<OrderContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<OrderContext> b, string cs)
        => b.EnableDetailedErrors();
}

public sealed class OrderSaver(OrderContext context)
{
    public Task<int> SaveWithoutAcceptingAsync() => context.SaveChangesAsync(false);

    public Task<int> SaveAsync() => context.SaveChangesAsync();
}

public sealed class StoredListConverters
{
    public ValueConverter<List<int>, string> ListConverter()
    {
        Func<ValueConverter<List<int>, string>> f = BuilderExtensions.BuildJsonListConvertor<List<int>>;
        return f();
    }

    public ValueConverter<IReadOnlyList<int>, string> ReadOnlyListConverter()
    {
        Func<ValueConverter<IReadOnlyList<int>, string>> f = BuilderExtensions.BuildJsonReadOnlyListConvertor<int>;
        return f();
    }
}

public sealed class Invoice : Entity<int>, IAggregateRoot<int>
{
    private readonly List<IDomainEvent> _events = [];

    public int CreatedById { get; init; }
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public int? LastModifiedById { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    IReadOnlyList<IDomainEvent> IAggregateRoot<int>.DomainEvents => _events;
    void IAggregateRoot<int>.AddDomainEvent(IDomainEvent eventItem) => _events.Add(eventItem);
    void IAggregateRoot<int>.ClearDomainEvents() => _events.Clear();
}
