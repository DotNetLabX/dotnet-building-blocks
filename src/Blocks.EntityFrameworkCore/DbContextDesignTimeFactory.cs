using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Blocks.EntityFrameworkCore;

public abstract class DesignTimeFactoryBase<TContext>
    : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var config = Host.CreateApplicationBuilder(args).Configuration;
        var cs = config.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Database'.");

        var builder = new DbContextOptionsBuilder<TContext>();
        
        ConfigureProvider(builder, cs);

        return CreateContext(builder.Options);
    }

    protected abstract void ConfigureProvider(DbContextOptionsBuilder<TContext> b, string cs);

    protected virtual TContext CreateContext(DbContextOptions<TContext> options)
    {
        var contextType = typeof(TContext);

        if (contextType.GetConstructor([typeof(DbContextOptions<TContext>)]) is { } optionsOnly)
            return (TContext)optionsOnly.Invoke([options]);

        if (contextType.GetConstructor([typeof(DbContextOptions<TContext>), typeof(IMemoryCache)]) is { } optionsAndCache)
            return (TContext)optionsAndCache.Invoke([options, new MemoryCache(new MemoryCacheOptions())]);

        var name = contextType.Name;
        throw new InvalidOperationException(
            $"{name} needs a constructor taking DbContextOptions<{name}>, or one taking DbContextOptions<{name}> and IMemoryCache.");
    }
}
