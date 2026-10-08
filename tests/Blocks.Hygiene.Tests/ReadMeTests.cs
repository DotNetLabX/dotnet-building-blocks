using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed class ReadMeTests
{
    private static readonly string[] BlockHeadings = ["## Purpose", "## Depends on", "## Registration"];

    private const string WayOut =
        "set the app's edit aside, set the app's manifest to the commit here that holds the other change, take the block forward, apply the edit again, then send it back";

    [Fact]
    public void EveryBlock_HasAReadMeWithTheThreeHeadings()
    {
        var blocks = Directory.EnumerateDirectories(Path.Combine(RepoFiles.Root, "src"), "Blocks.*").ToList();
        blocks.Should().HaveCount(11);

        var missing = blocks
            .SelectMany(block => MissingHeadings(Path.Combine(block, "README.md")).Select(heading => $"{Path.GetFileName(block)}: {heading}"))
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, missing).Should().BeEmpty();
    }

    [Fact]
    public void TheDataAccessReadMe_SaysBothSeedingHelpersNeedSqlServer()
    {
        var text = RepoFiles.Text(Path.Combine(RepoFiles.Root, "src", "Blocks.EntityFrameworkCore", "README.md"));

        text.Should().Contain("SQL Server only").And.Contain("ManualGenerateIdScope").And.Contain("TryReseedTable");
    }

    [Fact]
    public void TheRootReadMe_SaysHowToRunTheSyncTool()
    {
        var text = RepoFiles.Text(Path.Combine(RepoFiles.Root, "README.md")).ReplaceLineEndings(" ");
        var words = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        words.Should().Contain("blocks.json").And.Contain("-- forward").And.Contain("-- back").And.Contain("-- status")
            .And.Contain("--adopt").And.Contain(WayOut);
    }

    private static IEnumerable<string> MissingHeadings(string readMe)
    {
        if (!File.Exists(readMe))
            return ["README.md missing"];

        var lines = RepoFiles.Text(readMe).Split('\n').Select(l => l.TrimEnd('\r')).ToHashSet(StringComparer.Ordinal);
        return BlockHeadings.Where(heading => !lines.Contains(heading));
    }
}
