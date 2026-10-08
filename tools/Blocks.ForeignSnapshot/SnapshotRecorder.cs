using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Blocks.ForeignSnapshot;

public static class SnapshotRecorder
{
    public static string Record(IEnumerable<string> repos)
    {
        var text = new StringBuilder();
        foreach (var repo in repos)
        {
            text.Append("# repo\t").Append(repo).Append('\n');
            foreach (var line in RecordRepo(repo).Order(StringComparer.Ordinal))
                text.Append(line).Append('\n');
        }

        return text.ToString();
    }

    private static List<string> RecordRepo(string repo)
    {
        var lines = new List<string>
        {
            "head\t" + Git.Text(repo, "rev-parse", "HEAD").Trim()
        };

        foreach (var line in SplitLines(Git.Text(repo, "status", "--porcelain", "--ignored")))
            lines.Add("status\t" + line);

        foreach (var path in SplitNul(Git.Text(repo, "diff", "HEAD", "--name-only", "-z")))
            lines.Add($"diff\t{path}\t{Sha256(Git.Bytes(repo, "diff", "HEAD", "--binary", "--", path))}");

        foreach (var path in SplitNul(Git.Text(repo, "ls-files", "--others", "--exclude-standard", "-z")))
            lines.Add($"untracked\t{path}\t{Sha256(ReadShared(Path.Combine(repo, path)))}");

        foreach (var entry in SplitNul(Git.Text(repo, "status", "--porcelain", "--ignored", "-z")))
        {
            if (!entry.StartsWith("!! ", StringComparison.Ordinal))
                continue;

            var path = entry[3..];
            lines.Add($"ignored\t{path}\t{LastWriteUtc(Path.Combine(repo, path))}");
        }

        return lines;
    }

    private static string LastWriteUtc(string path)
    {
        var trimmed = path.TrimEnd('/', '\\');
        var time = Directory.Exists(trimmed)
            ? Directory.GetLastWriteTimeUtc(trimmed)
            : File.GetLastWriteTimeUtc(trimmed);
        return time.ToString("o", CultureInfo.InvariantCulture);
    }

    private static byte[] ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static IEnumerable<string> SplitLines(string text)
        => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r'));

    private static IEnumerable<string> SplitNul(string text)
        => text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
}
