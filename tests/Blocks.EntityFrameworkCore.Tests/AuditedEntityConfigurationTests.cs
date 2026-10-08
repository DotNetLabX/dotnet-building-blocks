using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Blocks.EntityFrameworkCore.Tests;

public sealed class AuditedEntityConfigurationTests
{
    [Fact]
    public void AuditedEntity_HasNoDatabaseSpecificDefaultOnCreatedOn()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();

        var createdOn = context.Model.FindEntityType(typeof(AuditedNote))!.FindProperty(nameof(AuditedNote.CreatedOn))!;

        createdOn.IsNullable.Should().BeFalse();
        createdOn.GetDefaultValueSql().Should().BeNull();
    }

    [Fact]
    public async Task AuditedEntity_SavesWithTheCreatedOnItCarries()
    {
        using var database = new TestDatabase();
        var created = new AuditedNote { Title = "audited" };
        await using (var context = database.CreateContext())
        {
            context.Add(created);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var fresh = database.CreateContext();
        var saved = await fresh.Set<AuditedNote>().SingleAsync(TestContext.Current.CancellationToken);

        saved.CreatedOn.Should().Be(created.CreatedOn);
    }
}
