using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Blocks.EntityFrameworkCore;

public class RepositoryBase<TContext, TEntity>(TContext dbContext)
    : RepositoryBase<TContext, TEntity, int>(dbContext)
    where TContext : DbContext
    where TEntity : class, IEntity<int>;

public abstract class RepositoryBase<TContext, TEntity, TKey>
    : IRepository<TEntity, TKey>
    where TContext : DbContext
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    protected readonly TContext _dbContext;
    protected readonly DbSet<TEntity> _entity;

    protected RepositoryBase(TContext db)
    {
        _dbContext = db;
        _entity = db.Set<TEntity>();
    }

	public TContext Context => _dbContext;

	protected DbSet<TEntity> Entity => _entity;
    public virtual IQueryable<TEntity> Query() => _entity;
    public virtual IQueryable<TEntity> QueryNotTracked() => _entity.AsNoTracking();

    public async Task<TEntity?> FindByIdAsync(TKey id) => await _entity.FindAsync(id);

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
        => await Query().SingleOrDefaultAsync(e => e.Id.Equals(id), ct);

    public virtual Task<bool> ExistsAsync(TKey id, CancellationToken ct = default)
        => _entity.AnyAsync(e => e.Id.Equals(id), ct);

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        => QueryNotTracked().AnyAsync(predicate, ct);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
        => (await _entity.AddAsync(entity, ct)).Entity;

    public virtual TEntity Update(TEntity entity) => _entity.Update(entity).Entity;

    public virtual async Task<TEntity> UpsertAsync(TEntity entity, CancellationToken ct = default)
    {
        var existingEntity = await FindByIdAsync(entity.Id);
        if (existingEntity is null)
        {
            return await AddAsync(entity, ct);
        }

        _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        return existingEntity;
    }

    public virtual void Remove(TEntity entity) => _entity.Remove(entity);

    public virtual async Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default)
    {
        // Raw SQL: marking a stub entity as Deleted is impossible when the entity has required properties.
        var rows = await _dbContext.Database.ExecuteSqlRawAsync(DeleteByIdStatement(), [id], ct);
        return rows > 0;
    }

    private string DeleteByIdStatement()
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Unknown entity {typeof(TEntity).Name}");
        var table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
            ?? throw new InvalidOperationException($"Table not mapped for {typeof(TEntity).Name}");
        var keyColumn = entityType.FindPrimaryKey()?.Properties.Single().GetColumnName(table)
            ?? throw new InvalidOperationException($"Key column not mapped for {typeof(TEntity).Name}");

        var sql = _dbContext.GetService<ISqlGenerationHelper>();
        return string.Concat(
            "DELETE FROM ", sql.DelimitIdentifier(table.Name, table.Schema),
            " WHERE ", sql.DelimitIdentifier(keyColumn), " = {0}");
    }

    public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => _entity.AddRangeAsync(entities, ct);

    public virtual void UpdateRange(IEnumerable<TEntity> entities) => _entity.UpdateRange(entities);

    public virtual void RemoveRange(IEnumerable<TEntity> entities) => _entity.RemoveRange(entities);

    public virtual Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);

    public virtual void ClearTracking() => _dbContext.ChangeTracker.Clear();

    public string TableName => _dbContext.Model.FindEntityType(typeof(TEntity))?.GetTableName()!;
}
