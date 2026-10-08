using System.Linq.Expressions;

namespace Blocks.EntityFrameworkCore;

public interface IRepository<TEntity> : IRepository<TEntity, int>
    where TEntity : class, IEntity<int>;

public interface IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    TEntity Update(TEntity entity);
    void Remove(TEntity entity);
    Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    void UpdateRange(IEnumerable<TEntity> entities);
    void RemoveRange(IEnumerable<TEntity> entities);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void ClearTracking();

    IQueryable<TEntity> Query();
    IQueryable<TEntity> QueryNotTracked();
}
