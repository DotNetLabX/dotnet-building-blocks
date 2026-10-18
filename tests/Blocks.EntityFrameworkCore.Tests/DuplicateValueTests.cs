using System.Data.Common;
using System.Net;
using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using Blocks.AspNetCore.Middlewares;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class DuplicateValueContext(DbContextOptions<DuplicateValueContext> options, IMemoryCache cache)
    : ApplicationDbContext<DuplicateValueContext>(options, cache)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new NoteConfiguration());
}

internal sealed class DuplicateValueNoteRepository(DuplicateValueContext dbContext)
    : RepositoryBase<DuplicateValueContext, Note>(dbContext);

internal sealed class ThrowingSaveInterceptor(Exception? thrown) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => thrown is null ? result : throw thrown;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => thrown is null ? ValueTask.FromResult(result) : throw thrown;
}

internal sealed class OtherDbException(string sqlState) : DbException("Another database's duplicate key.")
{
    public override string SqlState { get; } = sqlState;
}

internal sealed class NumberedDbException(int number) : DbException("Another database's duplicate key.")
{
    public int Number { get; } = number;
}

internal static class ProviderErrors
{
    public const string ProviderText = "Violation of UNIQUE KEY constraint 'UQ_Notes_Text'";

    public static SqlException SqlServer(int number)
    {
        var errorConstructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var error = (SqlError)errorConstructor.Invoke(errorConstructor.GetParameters().Select(p => p.Name switch
        {
            "infoNumber" => number,
            "errorMessage" => ProviderText,
            _ => p.ParameterType == typeof(string) ? "" : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray());

        var errors = (SqlErrorCollection)typeof(SqlErrorCollection)
            .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!
            .Invoke([]);
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(errors, [error]);

        var create = typeof(SqlException).GetMethod(
            "CreateException", BindingFlags.Static | BindingFlags.NonPublic, [typeof(SqlErrorCollection), typeof(string)])!;
        return (SqlException)create.Invoke(null, [errors, "16.0"])!;
    }

    public static PostgresException Postgres(string sqlState)
        => new($"{ProviderText} (PostgreSQL)", "ERROR", "ERROR", sqlState);

    public static DbUpdateException Failure(Exception inner)
        => new($"An error occurred while saving: {ProviderText}", inner);
}

public sealed class DuplicateValueTests : IDisposable
{
    private const string ConflictMessage = "A record with the same unique value already exists.";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public DuplicateValueTests()
    {
        _connection.Open();
        using var context = BaseContext(null);
        context.Database.EnsureCreated();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static TheoryData<string> Duplicates => ["sql 2601", "sql 2627", "legacy sql 2627", "postgres 23505"];

    public static TheoryData<string> NotDuplicates => ["sql 547", "legacy sql 547", "postgres 23503", "other 23505", "other number 2627"];

    private static Exception Inner(string name) => name switch
    {
        "sql 2601" => ProviderErrors.SqlServer(2601),
        "sql 2627" => ProviderErrors.SqlServer(2627),
        "sql 547" => ProviderErrors.SqlServer(547),
        "legacy sql 2627" => new global::System.Data.SqlClient.SqlException(2627),
        "legacy sql 547" => new global::System.Data.SqlClient.SqlException(547),
        "other number 2627" => new NumberedDbException(2627),
        "postgres 23505" => ProviderErrors.Postgres("23505"),
        "postgres 23503" => ProviderErrors.Postgres("23503"),
        "other 23505" => new OtherDbException("23505"),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Fact]
    public void ProviderErrors_CarryTheirCodes()
    {
        ProviderErrors.SqlServer(2627).Number.Should().Be(2627);
        ProviderErrors.Postgres("23505").SqlState.Should().Be("23505");
        new OtherDbException("23505").SqlState.Should().Be("23505");
    }

    [Theory]
    [MemberData(nameof(Duplicates))]
    public async Task BaseContext_AsyncSave_TurnsADuplicateIntoAConflict(string duplicate)
    {
        var failure = ProviderErrors.Failure(Inner(duplicate));
        await using var context = BaseContext(failure);
        context.Add(new Note(1, "a"));

        var save = () => context.SaveChangesAsync(Ct);

        await AssertConflictAsync(save, failure);
    }

    [Theory]
    [MemberData(nameof(Duplicates))]
    public void BaseContext_SyncSave_TurnsADuplicateIntoAConflict(string duplicate)
    {
        var failure = ProviderErrors.Failure(Inner(duplicate));
        using var context = BaseContext(failure);
        context.Add(new Note(1, "a"));

        var save = () => context.SaveChanges();

        var conflict = save.Should().Throw<ConflictException>().Which;
        conflict.Message.Should().Be(ConflictMessage);
        conflict.InnerException.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task BaseContext_SaveWithoutAccepting_TurnsADuplicateIntoAConflict()
    {
        var failure = ProviderErrors.Failure(ProviderErrors.SqlServer(2627));
        await using var context = BaseContext(failure);
        context.Add(new Note(1, "a"));

        await AssertConflictAsync(() => context.SaveChangesAsync(false, Ct), failure);
        context.Invoking(c => c.SaveChanges(false)).Should().Throw<ConflictException>();
    }

    [Fact]
    public async Task BaseContext_SaveWithoutAccepting_LeavesChangesUnaccepted()
    {
        await using var asyncContext = BaseContext(null);
        var asyncNote = new Note(1, "a");
        asyncContext.Add(asyncNote);
        await asyncContext.SaveChangesAsync(false, Ct);

        await using var syncContext = BaseContext(null);
        var syncNote = new Note(2, "b");
        syncContext.Add(syncNote);
        syncContext.SaveChanges(false);

        asyncContext.Entry(asyncNote).State.Should().Be(EntityState.Added);
        syncContext.Entry(syncNote).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task BaseContext_AsyncSave_HonoursTheToken()
    {
        await using var context = BaseContext(null);
        context.Add(new Note(1, "a"));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await context.Invoking(c => c.SaveChangesAsync(cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Repository_Save_HonoursTheToken()
    {
        await using var context = PlainContext(null);
        var repository = new NoteRepository(context);
        await repository.AddAsync(new Note(1, "a"), Ct);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await repository.Invoking(r => r.SaveChangesAsync(cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task BaseContext_DuplicateDeeperInTheChain_IsAConflict()
    {
        var failure = ProviderErrors.Failure(new InvalidOperationException("wrapper", ProviderErrors.Postgres("23505")));
        await using var context = BaseContext(failure);
        context.Add(new Note(1, "a"));

        await AssertConflictAsync(() => context.SaveChangesAsync(Ct), failure);
    }

    [Theory]
    [MemberData(nameof(NotDuplicates))]
    public async Task BaseContext_OtherFailures_PassUnchanged(string other)
    {
        var failure = ProviderErrors.Failure(Inner(other));
        await using var context = BaseContext(failure);
        context.Add(new Note(1, "a"));

        (await context.Invoking(c => c.SaveChangesAsync(Ct)).Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(failure);
        context.Invoking(c => c.SaveChanges()).Should().Throw<DbUpdateException>().Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task BaseContext_AnErrorThatIsNotASaveFailure_PassesUnchanged()
    {
        var thrown = new InvalidOperationException("not a save failure", ProviderErrors.SqlServer(2627));
        await using var context = BaseContext(thrown);
        context.Add(new Note(1, "a"));

        (await context.Invoking(c => c.SaveChangesAsync(Ct)).Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(thrown);
    }

    [Fact]
    public async Task BaseContext_RealSqliteUniqueViolation_PassesUnchanged()
    {
        await using (var first = BaseContext(null))
        {
            first.Add(new Note(1, "a"));
            await first.SaveChangesAsync(Ct);
        }

        await using var second = BaseContext(null);
        second.Add(new Note(1, "b"));
        (await second.Invoking(c => c.SaveChangesAsync(Ct)).Should().ThrowAsync<DbUpdateException>())
            .Which.InnerException.Should().BeOfType<SqliteException>();
        second.Invoking(c => c.SaveChanges()).Should().Throw<DbUpdateException>();
    }

    [Theory]
    [MemberData(nameof(Duplicates))]
    public async Task RepositoryOnAPlainContext_TurnsADuplicateIntoAConflict(string duplicate)
    {
        var failure = ProviderErrors.Failure(Inner(duplicate));
        await using var context = PlainContext(failure);
        var repository = new NoteRepository(context);
        await repository.AddAsync(new Note(1, "a"), Ct);

        await AssertConflictAsync(() => repository.SaveChangesAsync(Ct), failure);
    }

    [Theory]
    [MemberData(nameof(NotDuplicates))]
    public async Task RepositoryOnAPlainContext_OtherFailures_PassUnchanged(string other)
    {
        var failure = ProviderErrors.Failure(Inner(other));
        await using var context = PlainContext(failure);
        var repository = new NoteRepository(context);
        await repository.AddAsync(new Note(1, "a"), Ct);

        (await repository.Invoking(r => r.SaveChangesAsync(Ct)).Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task RepositoryOnAPlainContext_RealSqliteUniqueViolation_PassesUnchanged()
    {
        await using (var first = PlainContext(null))
        {
            first.Add(new Note(1, "a"));
            await first.SaveChangesAsync(Ct);
        }

        await using var second = PlainContext(null);
        var repository = new NoteRepository(second);
        await repository.AddAsync(new Note(1, "b"), Ct);
        await repository.Invoking(r => r.SaveChangesAsync(Ct)).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task RepositoryOnTheBaseContext_TurnsADuplicateIntoAConflict()
    {
        var failure = ProviderErrors.Failure(ProviderErrors.SqlServer(2601));
        await using var context = BaseContext(failure);
        var repository = new DuplicateValueNoteRepository(context);
        await repository.AddAsync(new Note(1, "a"), Ct);

        await AssertConflictAsync(() => repository.SaveChangesAsync(Ct), failure);
    }

    [Fact]
    public async Task PlainContextSavedDirectly_IsUnchanged()
    {
        var failure = ProviderErrors.Failure(ProviderErrors.SqlServer(2627));
        await using var context = PlainContext(failure);
        context.Add(new Note(1, "a"));

        (await context.Invoking(c => c.SaveChangesAsync(Ct)).Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(failure);
    }

    private static async Task AssertConflictAsync(Func<Task> save, DbUpdateException failure)
    {
        var conflict = (await save.Should().ThrowAsync<ConflictException>()).Which;
        conflict.Message.Should().Be(ConflictMessage);
        conflict.StatusCode.Should().Be(409);
        conflict.InnerException.Should().BeSameAs(failure);
    }

    private DuplicateValueContext BaseContext(Exception? thrown)
        => new(Options<DuplicateValueContext>(thrown), new MemoryCache(new MemoryCacheOptions()));

    private TestDbContext PlainContext(Exception? thrown) => new(Options<TestDbContext>(thrown));

    private DbContextOptions<TContext> Options<TContext>(Exception? thrown)
        where TContext : DbContext
        => new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ThrowingSaveInterceptor(thrown))
            .Options;

    public void Dispose() => _connection.Dispose();
}

public sealed class DuplicateValueReplyTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public DuplicateValueReplyTests()
    {
        _connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options);
        context.Database.EnsureCreated();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/context")]
    [InlineData("/repository")]
    public async Task Duplicate_Answers409WithTheFixedMessage(string path)
    {
        var (status, body) = await SendAsync(path, ProviderErrors.SqlServer(2627));

        status.Should().Be(HttpStatusCode.Conflict);
        JsonDocument.Parse(body).RootElement.GetProperty("message").GetString()
            .Should().Be("A record with the same unique value already exists.");
        body.Should().NotContain("UNIQUE KEY").And.NotContain("UQ_Notes_Text");
    }

    [Theory]
    [InlineData("/context")]
    [InlineData("/repository")]
    public async Task OtherSaveFailure_Answers500WithTheFixedSentence(string path)
    {
        var (status, body) = await SendAsync(path, ProviderErrors.SqlServer(547));

        status.Should().Be(HttpStatusCode.InternalServerError);
        JsonDocument.Parse(body).RootElement.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
        body.Should().NotContain("UNIQUE KEY").And.NotContain("UQ_Notes_Text");
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(string path, Exception inner)
    {
        var failure = ProviderErrors.Failure(inner);
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .UseEnvironment("Production")
                .Configure(app =>
                {
                    app.UseGlobalExceptionMiddleware();
                    app.Run(context => SaveAsync(context, failure));
                }))
            .StartAsync(Ct);

        using var client = host.GetTestClient();
        using var response = await client.GetAsync(path, Ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(Ct));
    }

    private async Task SaveAsync(HttpContext http, DbUpdateException failure)
    {
        if (http.Request.Path == "/context")
        {
            await using var context = new DuplicateValueContext(
                new DbContextOptionsBuilder<DuplicateValueContext>().UseSqlite(_connection).AddInterceptors(new ThrowingSaveInterceptor(failure)).Options,
                new MemoryCache(new MemoryCacheOptions()));
            context.Add(new Note(1, "a"));
            await context.SaveChangesAsync(http.RequestAborted);
            return;
        }

        await using var plain = new TestDbContext(
            new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).AddInterceptors(new ThrowingSaveInterceptor(failure)).Options);
        var repository = new NoteRepository(plain);
        await repository.AddAsync(new Note(1, "a"), http.RequestAborted);
        await repository.SaveChangesAsync(http.RequestAborted);
    }

    public void Dispose() => _connection.Dispose();
}
