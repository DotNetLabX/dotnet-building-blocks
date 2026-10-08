using Blocks.EntityFrameworkCore;
using Blocks.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class Note : Entity
{
    private string _text = string.Empty;

    private Note() { }

    public Note(int id, string text)
    {
        Id = id;
        _text = text;
    }

    public string ReadText() => _text;
}

public sealed class AuditedNote : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
}

internal sealed class NoteConfiguration : EntityConfiguration<Note>
{
    public override void Configure(EntityTypeBuilder<Note> builder)
    {
        base.Configure(builder);
        builder.Property<string>("_text").HasColumnName("Text").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AuditedNoteConfiguration : AuditedEntityConfiguration<AuditedNote>
{
    public override void Configure(EntityTypeBuilder<AuditedNote> builder)
    {
        base.Configure(builder);
        builder.Ignore(e => e.DomainEvents);
    }
}

public sealed class Folder : Entity
{
    public string Name { get; set; } = string.Empty;
}

public sealed class FiledNote : Entity
{
    public string Title { get; set; } = string.Empty;

    public Folder? Folder { get; set; }
}

internal sealed class FiledNoteConfiguration : EntityConfiguration<FiledNote>
{
    public const string FolderKey = "FolderId";
    public const string Tag = "Tag";

    public override void Configure(EntityTypeBuilder<FiledNote> builder)
    {
        base.Configure(builder);
        builder.HasOne(e => e.Folder).WithMany().HasForeignKey(FolderKey).IsRequired(false);
        builder.Property<string?>(Tag);
    }
}

public sealed class Label : Entity
{
    public string Name { get; set; } = string.Empty;
}

internal sealed class LabelConfiguration : EntityConfiguration<Label>
{
    public const string KeyColumn = "LabelKey";

    public override void Configure(EntityTypeBuilder<Label> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Id).HasColumnName(KeyColumn);
    }
}

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NoteConfiguration());
        modelBuilder.ApplyConfiguration(new AuditedNoteConfiguration());
        modelBuilder.ApplyConfiguration(new FiledNoteConfiguration());
        modelBuilder.Entity<Folder>();
        modelBuilder.ApplyConfiguration(new LabelConfiguration());
    }
}

internal sealed class NoteRepository(TestDbContext dbContext) : RepositoryBase<TestDbContext, Note>(dbContext);

internal sealed class AuditedNoteRepository(TestDbContext dbContext) : RepositoryBase<TestDbContext, AuditedNote>(dbContext);

internal sealed class FiledNoteRepository(TestDbContext dbContext) : RepositoryBase<TestDbContext, FiledNote>(dbContext);

internal sealed class LabelRepository(TestDbContext dbContext) : RepositoryBase<TestDbContext, Label>(dbContext);

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TestDbContext CreateContext()
        => new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
