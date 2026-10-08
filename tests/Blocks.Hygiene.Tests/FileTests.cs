using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed class FileTests
{
    private static readonly string[] SuppressionForms =
    [
        "#pragma " + "warning disable",
        "Suppress" + "Message",
        "No" + "Warn",
        "WarningsNot" + "AsErrors",
    ];

    [Fact]
    public void NoFileInTheRepo_StartsWithAByteOrderMark()
    {
        var hits = RepoFiles.Under("")
            .Where(file => File.ReadAllBytes(file).AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
            .Select(RepoFiles.Relative)
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, hits).Should().BeEmpty();
    }

    [Fact]
    public void NoFileInTheRepo_SuppressesAWarning()
    {
        var hits = RepoFiles.Under("")
            .Where(file => !file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => RepoFiles.Text(file)
                .Split('\n')
                .Select((text, index) => (File: RepoFiles.Relative(file), Number: index + 1, Text: text)))
            .Where(line => SuppressionForms.Any(form => line.Text.Contains(form, StringComparison.OrdinalIgnoreCase)))
            .Select(line => $"{line.File}:{line.Number}: {line.Text.Trim()}");

        string.Join(Environment.NewLine, hits).Should().BeEmpty();
    }
}
