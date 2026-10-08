using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Blocks.EntityFrameworkCore.Seeding;

public sealed class ManualGenerateIdScope<TEntity> : IDisposable 
    where TEntity : class
{
    private readonly DbContext _dbContext;
    private readonly string? _tableName;
    private readonly bool _enabled;

    public ManualGenerateIdScope(DbContext dbContext, bool enabled)
    {
        _dbContext = dbContext;
        _enabled = enabled;

        if (!enabled) return;

        var entity = dbContext.Model.FindEntityType(typeof(TEntity))
                 ?? throw new InvalidOperationException($"Unknown entity {typeof(TEntity).Name}");
        var schema = entity.GetSchema() ?? "dbo";
        var table = entity.GetTableName()
                 ?? throw new InvalidOperationException($"Table not mapped for {typeof(TEntity).Name}");

        _tableName = dbContext.GetService<ISqlGenerationHelper>().DelimitIdentifier(table, schema);
        var statement = string.Concat("SET IDENTITY_INSERT ", _tableName, " ON");
        _dbContext.Database.ExecuteSqlRaw(statement);
    }

    public static ManualGenerateIdScope<TEntity> Create(DbContext ctx, bool enabled)
        => new(ctx, enabled);

    public void Dispose()
    {
        if (!_enabled || _tableName is null)
            return;

        var statement = string.Concat("SET IDENTITY_INSERT ", _tableName, " OFF");
        _dbContext.Database.ExecuteSqlRaw(statement);
    }
}