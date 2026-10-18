using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class CachedCatalogContext(DbContextOptions<CachedCatalogContext> options, IMemoryCache cache)
    : ApplicationDbContext<CachedCatalogContext>(options, cache)
{
    public IMemoryCache Cache { get; } = cache;
}

public sealed class DualCatalogContext : DbContext
{
    public DualCatalogContext(DbContextOptions<DualCatalogContext> options) : base(options) { }

    public DualCatalogContext(DbContextOptions<DualCatalogContext> options, IMemoryCache cache) : base(options) => Cache = cache;

    public IMemoryCache? Cache { get; }
}

public sealed class PlainCatalogContext(DbContextOptions<PlainCatalogContext> options) : DbContext(options);

public sealed class LoosePlainCatalogContext(DbContextOptions options) : DbContext(options);

public sealed class NamedCatalogContext(DbContextOptions<NamedCatalogContext> options, string name) : DbContext(options)
{
    public string Name { get; } = name;
}

public sealed class LabelledCatalogContext(string label) : DbContext
{
    public string Label { get; } = label;
}

internal sealed class LabelledCatalogContextFactory : DesignTimeFactoryBase<LabelledCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<LabelledCatalogContext> b, string cs) => b.UseSqlite(cs);
}

internal sealed class CachedCatalogContextFactory : DesignTimeFactoryBase<CachedCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<CachedCatalogContext> b, string cs) => b.UseSqlite(cs);
}

internal sealed class DualCatalogContextFactory : DesignTimeFactoryBase<DualCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<DualCatalogContext> b, string cs) => b.UseSqlite(cs);
}

internal sealed class PlainCatalogContextFactory : DesignTimeFactoryBase<PlainCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<PlainCatalogContext> b, string cs) => b.UseSqlite(cs);
}

internal sealed class LoosePlainCatalogContextFactory : DesignTimeFactoryBase<LoosePlainCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<LoosePlainCatalogContext> b, string cs) => b.UseSqlite(cs);
}

internal sealed class NamedCatalogContextFactory : DesignTimeFactoryBase<NamedCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<NamedCatalogContext> b, string cs) => b.UseSqlite(cs);
}

internal sealed class NamedCatalogContextHookFactory : DesignTimeFactoryBase<NamedCatalogContext>
{
    protected override void ConfigureProvider(DbContextOptionsBuilder<NamedCatalogContext> b, string cs) => b.UseSqlite(cs);

    protected override NamedCatalogContext CreateContext(DbContextOptions<NamedCatalogContext> options) => new(options, "from the hook");
}

public sealed class DesignTimeFactoryTests
{
    private const string ConnectionString = "DataSource=:memory:";

    private static readonly string[] Args = [$"--ConnectionStrings:Database={ConnectionString}"];

    [Fact]
    public void ContextOnTheBlocksBase_IsBuiltWithAMemoryCache()
    {
        using var context = new CachedCatalogContextFactory().CreateDbContext(Args);

        context.Should().BeOfType<CachedCatalogContext>();
        context.Cache.Should().NotBeNull();
        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
        context.Database.GetConnectionString().Should().Be(ConnectionString);
    }

    [Fact]
    public void ContextTakingTheOptionsAlone_IsBuiltAsToday()
    {
        using var context = new PlainCatalogContextFactory().CreateDbContext(Args);

        context.Database.GetConnectionString().Should().Be(ConnectionString);
    }

    [Fact]
    public void ContextWithBothConstructors_IsBuiltThroughTheOptionsOnlyOne()
    {
        using var context = new DualCatalogContextFactory().CreateDbContext(Args);

        context.Cache.Should().BeNull();
        context.Database.GetConnectionString().Should().Be(ConnectionString);
    }

    [Fact]
    public void ContextTakingTheNonGenericOptions_IsBuiltAsToday()
    {
        using var context = new LoosePlainCatalogContextFactory().CreateDbContext(Args);

        context.Database.GetConnectionString().Should().Be(ConnectionString);
    }

    [Fact]
    public void ContextWithAnotherConstructor_IsRefusedNamingBothShapes()
    {
        var create = () => new NamedCatalogContextFactory().CreateDbContext(Args);

        create.Should().Throw<InvalidOperationException>().WithMessage(
            "NamedCatalogContext needs a constructor taking DbContextOptions<NamedCatalogContext>, " +
            "or one taking DbContextOptions<NamedCatalogContext> and IMemoryCache.");
    }

    [Fact]
    public void ContextWhoseOneParameterIsNotTheOptions_IsRefusedNamingBothShapes()
    {
        var create = () => new LabelledCatalogContextFactory().CreateDbContext(Args);

        create.Should().Throw<InvalidOperationException>().WithMessage(
            "LabelledCatalogContext needs a constructor taking DbContextOptions<LabelledCatalogContext>, " +
            "or one taking DbContextOptions<LabelledCatalogContext> and IMemoryCache.");
    }

    [Fact]
    public void OverriddenCreationHook_BuildsTheContextWithTheConfiguredOptions()
    {
        using var context = new NamedCatalogContextHookFactory().CreateDbContext(Args);

        context.Name.Should().Be("from the hook");
        context.Database.GetConnectionString().Should().Be(ConnectionString);
    }

    [Fact]
    public void MissingConnectionString_IsRefused()
    {
        var create = () => new PlainCatalogContextFactory().CreateDbContext([]);

        create.Should().Throw<InvalidOperationException>().WithMessage("Missing 'ConnectionStrings:Database'.");
    }
}
