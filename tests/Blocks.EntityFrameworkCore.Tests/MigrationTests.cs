using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class MigrationTestContext(DbContextOptions<MigrationTestContext> options) : DbContext(options);

[DbContext(typeof(MigrationTestContext))]
[Migration(Id)]
public sealed class CreateGadgets : Migration
{
    public const string Id = "20261018000000_CreateGadgets";

    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.CreateTable(
            name: "Gadgets",
            columns: table => new { Id = table.Column<int>(nullable: false) },
            constraints: table => table.PrimaryKey("PK_Gadgets", x => x.Id));

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Gadgets");
}

public sealed class MigrationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly IHost _host;

    public MigrationTests()
    {
        _connection.Open();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDbContext<MigrationTestContext>(options => options
            .UseSqlite(_connection)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        _host = builder.Build();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MigrateAsync_AppliesTheMigrationAndReturnsTheHost()
    {
        var returned = await _host.MigrateAsync<MigrationTestContext>(Ct);

        returned.Should().BeSameAs(_host);
        (await AppliedAsync()).Should().Equal(CreateGadgets.Id);
    }

    [Fact]
    public async Task MigrateAsync_WithACancelledToken_AppliesNothing()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var migrate = () => _host.MigrateAsync<MigrationTestContext>(cancelled.Token);

        await migrate.Should().ThrowAsync<OperationCanceledException>();
        (await AppliedAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Migrate_StillAppliesTheMigration()
    {
        _host.Migrate<MigrationTestContext>().Should().BeSameAs(_host);

        (await AppliedAsync()).Should().Equal(CreateGadgets.Id);
    }

    private async Task<IReadOnlyList<string>> AppliedAsync()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MigrationTestContext>();
        return (await context.Database.GetAppliedMigrationsAsync(Ct)).ToList();
    }

    public void Dispose()
    {
        _host.Dispose();
        _connection.Dispose();
    }
}
