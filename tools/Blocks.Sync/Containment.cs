using System.Text.RegularExpressions;

namespace Blocks.Sync;

internal static partial class Containment
{
    [GeneratedRegex(@"^Blocks\.[A-Za-z0-9]+(\.[A-Za-z0-9]+)*$")]
    private static partial Regex BlockName();

    public static void CheckBlockName(string name, string where)
    {
        if (!BlockName().IsMatch(name))
            throw new RefusedException($"refused: block name '{name}' in {where} is not a block name (Blocks.Name)");
    }

    public static string CheckRelative(string path, string what)
    {
        if (path.Length == 0
            || path.Contains(':')
            || Path.IsPathRooted(path)
            || path.StartsWith('/') || path.StartsWith('\\'))
        {
            throw new RefusedException($"refused: {what} '{path}' must be a relative path with no '..' segment and no root");
        }

        var segments = path.Replace('\\', '/').Split('/').Where(s => s is not ("" or ".")).ToList();
        if (segments.Contains(".."))
            throw new RefusedException($"refused: {what} '{path}' must be a relative path with no '..' segment and no root");
        if (segments.Count == 0)
            throw new RefusedException($"refused: {what} '{path}' names no folder inside the app");

        return string.Join('/', segments);
    }

    public static string Inside(string root, string relative, string what)
    {
        var full = Path.GetFullPath(Path.Combine(root, CheckRelative(relative, what)));
        if (!full.StartsWith(WithSeparator(root), StringComparison.OrdinalIgnoreCase))
            throw new RefusedException($"refused: {what} '{relative}' resolves outside {root}");
        return full;
    }

    public static void CheckNoLinks(string root, string path, string what)
    {
        var folders = new List<string> { Path.GetFullPath(root) };
        foreach (var segment in Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Where(s => s != "."))
            folders.Add(Path.Combine(folders[^1], segment));

        foreach (var folder in folders.TakeWhile(Directory.Exists))
        {
            if (new DirectoryInfo(folder).Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new RefusedException($"refused: {what}: {folder} is a symbolic link or junction");
        }
    }

    private static string WithSeparator(string root) => Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
}
