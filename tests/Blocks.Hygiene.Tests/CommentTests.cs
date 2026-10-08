using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed partial class CommentTests
{
    private static readonly string[] CheckedFolders = ["src", "tools"];

    [GeneratedRegex(@"//\s*(insight|talk|todo)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Marker();

    [GeneratedRegex(@"^\s*//(?!/)\s*(?<text>.*?)\s*$")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"^[^/""]*[^/""\s]\s*//(?!/)\s*(?<text>.*?)\s*$")]
    private static partial Regex TrailingComment();

    [GeneratedRegex(@"^(:\s*[A-Za-z_][\w.<>, ]*$|\.[A-Za-z_]\w*\()")]
    private static partial Regex DeclarationOrCallShape();

    [GeneratedRegex(@"^(if|else|for|foreach|while|do|switch|case|return|var|throw|try|catch|finally|using|break|continue|goto|yield|await|lock)\b")]
    private static partial Regex StatementKeyword();

    [Fact]
    public void NoSourceFile_CarriesACourseNoteOrToDoMarker()
    {
        var hits = SourceLines().Where(l => Marker().IsMatch(l.Text)).Select(Describe);

        string.Join(Environment.NewLine, hits).Should().BeEmpty();
    }

    [Fact]
    public void NoSourceFile_CarriesCommentedOutCode()
    {
        var hits = SourceLines()
            .Select(l => (Line: l, Match: LineComment().Match(l.Text) is { Success: true } whole ? whole : TrailingComment().Match(l.Text)))
            .Where(l => l.Match.Success && LooksLikeCode(l.Match.Groups["text"].Value))
            .Select(l => Describe(l.Line));

        string.Join(Environment.NewLine, hits).Should().BeEmpty();
    }

    private static bool LooksLikeCode(string text)
        => text.EndsWith(';') || text.EndsWith('{') || text.EndsWith('}') || StatementKeyword().IsMatch(text) || DeclarationOrCallShape().IsMatch(text);

    private static IEnumerable<(string File, int Number, string Text)> SourceLines()
        => CheckedFolders
            .SelectMany(folder => RepoFiles.Under(folder, "*.cs"))
            .SelectMany(file => RepoFiles.Text(file)
                .Split('\n')
                .Select((text, index) => (File: RepoFiles.Relative(file), Number: index + 1, Text: text.TrimEnd('\r'))));

    private static string Describe((string File, int Number, string Text) line) => $"{line.File}:{line.Number}: {line.Text.Trim()}";
}
