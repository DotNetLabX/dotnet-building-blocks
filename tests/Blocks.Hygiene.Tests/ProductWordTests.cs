using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed class ProductWordTests
{
    private static readonly string[] ProductWords = ["article", "journal"];

    [Fact]
    public void NoFileInSrc_ContainsAProductWord()
    {
        var hits = RepoFiles.Under("src")
            .SelectMany(file => RepoFiles.Text(file)
                .Split('\n')
                .Select((line, index) => (File: RepoFiles.Relative(file), Line: index + 1, Text: line)))
            .Where(l => ProductWords.Any(word => l.Text.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}");

        string.Join(Environment.NewLine, hits).Should().BeEmpty();
    }
}
