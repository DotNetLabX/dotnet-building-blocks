using Blocks.Core.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blocks.EntityFrameworkCore;

public static partial class DbContextExtensions
{
    public static void UnTrackCacheableEntities(this DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is ICacheable)
                entry.State = EntityState.Unchanged;
        }
    }

    public static IHost Migrate<TDbContext>(this IHost app)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        context.Database.Migrate();
        return app;
    }

    public static async Task<IHost> MigrateAsync<TDbContext>(this IHost app, CancellationToken ct = default)
        where TDbContext : DbContext
    {
        await using var scope = app.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        await context.Database.MigrateAsync(ct);
        return app;
    }
}
