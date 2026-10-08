namespace Blocks.Sync;

internal static class SafeFiles
{
    public static void Write(string path, byte[] content)
    {
        var folder = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(folder);
        var temporary = Path.Combine(folder, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporary, content);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    public static void Delete(string root, string path)
    {
        File.Delete(path);
        var stop = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        for (var folder = Path.GetDirectoryName(path); folder is not null && folder.Length > stop.Length; folder = Path.GetDirectoryName(folder))
        {
            if (Directory.EnumerateFileSystemEntries(folder).Any())
                return;
            Directory.Delete(folder);
        }
    }

    public static void CheckReplaceable(string folder, BlockVersion current, BlockVersion wanted, string block)
    {
        var removed = Removed(current, wanted).ToHashSet(StringComparer.Ordinal);
        foreach (var path in wanted.Files.Keys)
        {
            var segments = path.Split('/');
            for (var i = 1; i < segments.Length; i++)
            {
                var ancestor = string.Join('/', segments[..i]);
                if (File.Exists(Path.Combine(folder, ancestor)) && !removed.Contains(ancestor))
                    throw new RefusedException(Messages.FileInTheWay(block, path, ancestor));
            }

            var target = Path.Combine(folder, path);
            if (!Directory.Exists(target))
                continue;

            var kept = EntriesUnder(folder, target).Where(entry => !removed.Contains(entry)).Order(StringComparer.Ordinal).ToList();
            if (kept.Count > 0)
                throw new RefusedException(Messages.FolderInTheWay(block, path, kept));
        }
    }

    public static void Mirror(string folder, BlockVersion current, BlockVersion wanted, Func<byte[], byte[]> transform)
    {
        foreach (var path in Removed(current, wanted))
            Delete(folder, Path.Combine(folder, path));

        foreach (var path in wanted.Files.Keys)
        {
            var target = Path.Combine(folder, path);
            if (Directory.Exists(target))
                DeleteEmptyFolders(target);
            var content = transform(wanted.Content(path));
            if (!File.Exists(target) || !File.ReadAllBytes(target).AsSpan().SequenceEqual(content))
                Write(target, content);
        }
    }

    private static void DeleteEmptyFolders(string directory)
    {
        foreach (var child in new DirectoryInfo(directory).EnumerateDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.ReparsePoint)))
            DeleteEmptyFolders(child.FullName);
        Directory.Delete(directory);
    }

    private static IEnumerable<string> Removed(BlockVersion current, BlockVersion wanted)
        => current.Files.Keys.Where(p => !wanted.Files.ContainsKey(p)).ToList();

    private static IEnumerable<string> EntriesUnder(string folder, string directory)
    {
        var pending = new Stack<DirectoryInfo>([new DirectoryInfo(directory)]);
        while (pending.Count > 0)
        {
            foreach (var entry in pending.Pop().EnumerateFileSystemInfos())
            {
                if (entry is DirectoryInfo child && !child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    pending.Push(child);
                else
                    yield return Path.GetRelativePath(folder, entry.FullName).Replace('\\', '/');
            }
        }
    }
}
