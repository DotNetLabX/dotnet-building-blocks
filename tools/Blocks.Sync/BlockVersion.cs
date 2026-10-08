using System.Security.Cryptography;
using System.Text;

namespace Blocks.Sync;

internal sealed class BlockVersion
{
    private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", ".vscode", ".idea" };
    private static readonly byte[] ByteOrderMark = [0xEF, 0xBB, 0xBF];

    private readonly Dictionary<string, byte[]> _contents;

    private BlockVersion(Dictionary<string, byte[]> contents)
    {
        _contents = contents;
        Files = new SortedDictionary<string, string>(
            contents.ToDictionary(c => c.Key, c => Fingerprint(c.Value), StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    public static BlockVersion Empty { get; } = new([]);

    public SortedDictionary<string, string> Files { get; }

    public bool IsEmpty => Files.Count == 0;

    public byte[] Content(string path) => _contents[path];

    public static bool IsInFileSet(string relativePath)
    {
        var segments = relativePath.Split('/');
        if (segments[..^1].Any(ExcludedFolders.Contains))
            return false;

        var name = segments[^1];
        return !name.EndsWith(".user", StringComparison.OrdinalIgnoreCase)
            && !name.EndsWith(".suo", StringComparison.OrdinalIgnoreCase)
            && !name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase);
    }

    public static byte[] WithoutByteOrderMark(byte[] content)
        => content.AsSpan().StartsWith(ByteOrderMark) ? content[ByteOrderMark.Length..] : content;

    public static string Fingerprint(byte[] content)
    {
        var text = WithoutByteOrderMark(content);
        var normalized = new List<byte>(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                continue;
            normalized.Add(text[i]);
        }

        return Convert.ToHexStringLower(SHA256.HashData(normalized.ToArray()));
    }

    public static bool Same(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
        => left.Count == right.Count && left.All(l => right.TryGetValue(l.Key, out var r) && r == l.Value);

    public static List<string> Changes(IReadOnlyDictionary<string, string> from, IReadOnlyDictionary<string, string> to)
    {
        var changes = new List<string>();
        foreach (var path in from.Keys.Union(to.Keys).Order(StringComparer.Ordinal))
        {
            var before = from.GetValueOrDefault(path);
            var after = to.GetValueOrDefault(path);
            if (before == after)
                continue;
            changes.Add(before is null ? $"added {path}" : after is null ? $"removed {path}" : $"changed {path}");
        }

        return changes;
    }

    public static BlockVersion FromFolder(string folder)
    {
        if (!Directory.Exists(folder))
            return Empty;

        var contents = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var pending = new Stack<DirectoryInfo>([new DirectoryInfo(folder)]);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new RefusedException($"refused: {directory.FullName} is a symbolic link or junction");

            foreach (var file in directory.EnumerateFiles())
            {
                var relative = Path.GetRelativePath(folder, file.FullName).Replace('\\', '/');
                if (IsInFileSet(relative))
                    contents[relative] = File.ReadAllBytes(file.FullName);
            }

            foreach (var child in directory.EnumerateDirectories())
            {
                if (!ExcludedFolders.Contains(child.Name))
                    pending.Push(child);
            }
        }

        return new BlockVersion(contents);
    }

    public static BlockVersion FromCommit(string repo, string commit, string block)
    {
        var prefix = $"src/{block}/";
        var contents = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var listing = Encoding.UTF8.GetString(Git.Bytes(repo, "ls-tree", "-r", "-z", commit, "--", prefix));
        foreach (var record in listing.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = record.IndexOf('\t');
            var fields = record[..tab].Split(' ');
            var path = record[(tab + 1)..];
            if (fields[1] != "blob" || !path.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var relative = path[prefix.Length..];
            if (IsInFileSet(relative))
                contents[relative] = Git.Bytes(repo, "cat-file", "blob", fields[2]);
        }

        return new BlockVersion(contents);
    }
}
