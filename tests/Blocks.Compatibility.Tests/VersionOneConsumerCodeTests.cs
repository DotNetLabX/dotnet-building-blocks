using AwesomeAssertions;
using Blocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Blocks.Compatibility.Tests;

public sealed class VersionOneConsumerCodeTests
{
    [Fact]
    public void EveryVersionOneConsumerType_CanBeCreated()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var context = new OrderContext(new DbContextOptionsBuilder<OrderContext>().Options, cache);
        var order = new Order();
        var shipment = new Shipment();
        new AuditReset().ClearLastModifiedBy(order, shipment);
        var converters = new StoredListConverters();

        object[] created =
        [
            order,
            shipment,
            new OrderConfiguration(),
            new ShipmentConfiguration(),
            new FixedClaimsProvider(),
            new PlaceOrderCommand(),
            new RenameOrderCommand(),
            new OrderContextFactory(),
            new OrderSaver(context),
            converters.ListConverter(),
            converters.ReadOnlyListConverter(),
            new Invoice(),
        ];

        created.Should().AllSatisfy(item => item.Should().NotBeNull());
        order.LastModifiedById.Should().BeNull();
        shipment.LastModifiedById.Should().BeNull();
        ((IAuditableAction)new PlaceOrderCommand()).Action.Should().Be(nameof(OrderAction.Placed));
    }
}
