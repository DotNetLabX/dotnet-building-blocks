using Blocks.Core.Cache;
using Mapster;
using Microsoft.Extensions.Caching.Memory;

namespace Blocks.EntityFrameworkCore;

public class ApplicationDbContext<TDbContext>(DbContextOptions<TDbContext> _options, IMemoryCache _cache) 
    : DbContext(_options)
    where TDbContext : DbContext
{
    public virtual IEnumerable<TEntity> GetAllCached<TEntity>()
        where TEntity : class, ICacheable
        => _cache.GetOrCreateByType(entry => this.Set<TEntity>().AsNoTracking().ToList());

    public virtual TEntity GetByIdCached<TEntity, TId>(TId id)
        where TEntity : class, IEntity<TId>, ICacheable
        where TId : struct
        => GetAllCached<TEntity>().Single(e => e.Id.Equals(id));

    public virtual IEnumerable<TDestination> GetAllCached<TEntity, TDestination>()
        where TEntity : class, ICacheable
        => _cache.GetOrCreateByType(entry => this.Set<TEntity>().AsNoTracking().ProjectToType<TDestination>().ToList());

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateValue())
        {
            throw ex.ToConflict();
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateValue())
        {
            throw ex.ToConflict();
        }
    }
}
