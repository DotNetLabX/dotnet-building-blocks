using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Blocks.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class RepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EnsureNotExistsOrThrow_ThrowsConflictForAnExistingId()
    {
        using var database = new TestDatabase();
        await SeedAsync(database, new Note(1, "first"));
        await using var context = database.CreateContext();
        var repository = new NoteRepository(context);

        var act = () => repository.EnsureNotExistsOrThrowAsync(1, Ct);

        var thrown = await act.Should().ThrowExactlyAsync<ConflictException>();
        thrown.Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task EnsureNotExistsOrThrow_PassesForAMissingId()
    {
        using var database = new TestDatabase();
        await SeedAsync(database, new Note(1, "first"));
        await using var context = database.CreateContext();
        var repository = new NoteRepository(context);

        var act = () => repository.EnsureNotExistsOrThrowAsync(2, Ct);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Upsert_UpdatesAPropertyStoredInAPrivateField()
    {
        using var database = new TestDatabase();
        await SeedAsync(database, new Note(1, "old"));
        await using (var context = database.CreateContext())
        {
            var repository = new NoteRepository(context);
            await repository.UpsertAsync(new Note(1, "new"), Ct);
            await repository.SaveChangesAsync(Ct);
        }

        await using var fresh = database.CreateContext();
        var saved = await fresh.Set<Note>().SingleAsync(Ct);

        saved.ReadText().Should().Be("new");
    }

    [Fact]
    public async Task Upsert_AddsAnEntityThatDoesNotExist()
    {
        using var database = new TestDatabase();
        await SeedAsync(database, new Note(1, "first"));
        await using (var context = database.CreateContext())
        {
            var repository = new NoteRepository(context);
            await repository.UpsertAsync(new Note(2, "second"), Ct);
            await repository.SaveChangesAsync(Ct);
        }

        await using var fresh = database.CreateContext();
        var texts = await fresh.Set<Note>().OrderBy(n => n.Id).Select(n => EF.Property<string>(n, "_text")).ToListAsync(Ct);

        texts.Should().Equal("first", "second");
    }

    [Fact]
    public async Task Upsert_KeepsShadowPropertyValues()
    {
        using var database = new TestDatabase();
        await using (var context = database.CreateContext())
        {
            var note = new FiledNote { Id = 1, Title = "old", Folder = new Folder { Id = 7, Name = "inbox" } };
            context.Add(note);
            context.Entry(note).Property(FiledNoteConfiguration.Tag).CurrentValue = "pinned";
            await context.SaveChangesAsync(Ct);
        }

        await using (var context = database.CreateContext())
        {
            var repository = new FiledNoteRepository(context);
            await repository.UpsertAsync(new FiledNote { Id = 1, Title = "new" }, Ct);
            await repository.SaveChangesAsync(Ct);
        }

        await using var fresh = database.CreateContext();
        var saved = await fresh.Set<FiledNote>()
            .Select(n => new
            {
                n.Title,
                FolderId = EF.Property<int?>(n, FiledNoteConfiguration.FolderKey),
                Tag = EF.Property<string?>(n, FiledNoteConfiguration.Tag),
            })
            .SingleAsync(Ct);

        using var scope = new AssertionScope();
        saved.Title.Should().Be("new");
        saved.FolderId.Should().Be(7);
        saved.Tag.Should().Be("pinned");
    }

    [Fact]
    public async Task DeleteById_RemovesOnlyThatEntityAndReturnsTrue()
    {
        using var database = new TestDatabase();
        await SeedAsync(database, new Note(1, "first"), new Note(2, "second"));
        bool deleted;
        await using (var context = database.CreateContext())
        {
            deleted = await new NoteRepository(context).DeleteByIdAsync(1, Ct);
        }

        await using var fresh = database.CreateContext();
        var remaining = await fresh.Set<Note>().Select(n => n.Id).ToListAsync(Ct);

        deleted.Should().BeTrue();
        remaining.Should().Equal(2);
    }

    [Fact]
    public async Task DeleteById_ReturnsFalseForAMissingId()
    {
        using var database = new TestDatabase();
        await SeedAsync(database, new Note(1, "first"));
        await using var context = database.CreateContext();

        var deleted = await new NoteRepository(context).DeleteByIdAsync(9, Ct);

        deleted.Should().BeFalse();
        (await context.Set<Note>().CountAsync(Ct)).Should().Be(1);
    }

    [Fact]
    public async Task DeleteById_UsesTheKeyColumnTheModelMaps()
    {
        using var database = new TestDatabase();
        await using (var seed = database.CreateContext())
        {
            seed.AddRange(new Label { Id = 1, Name = "first" }, new Label { Id = 2, Name = "second" });
            await seed.SaveChangesAsync(Ct);
        }

        bool deleted;
        await using (var context = database.CreateContext())
        {
            deleted = await new LabelRepository(context).DeleteByIdAsync(1, Ct);
        }

        await using var fresh = database.CreateContext();
        deleted.Should().BeTrue();
        (await fresh.Set<Label>().Select(l => l.Id).ToListAsync(Ct)).Should().Equal(2);
        fresh.Model.FindEntityType(typeof(Label))!.FindPrimaryKey()!.Properties[0].GetColumnName().Should().Be(LabelConfiguration.KeyColumn);
    }

    private static async Task SeedAsync(TestDatabase database, params Note[] notes)
    {
        await using var context = database.CreateContext();
        context.AddRange(notes);
        await context.SaveChangesAsync(Ct);
    }
}
