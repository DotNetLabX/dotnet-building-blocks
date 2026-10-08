using System.Text;

namespace Blocks.Hygiene.Tests;

internal static class RepoFiles
{
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", ".claude" };

    public static string Root { get; } = FindRoot();

    public static IEnumerable<string> Under(string relativeFolder, string pattern = "*")
    {
        var start = Path.Combine(Root, relativeFolder);
        var pending = new Stack<string>([start]);
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(folder, pattern))
                yield return file;
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                if (!SkippedFolders.Contains(Path.GetFileName(child)))
                    pending.Push(child);
            }
        }
    }

    public static string Text(string path) => Encoding.UTF8.GetString(File.ReadAllBytes(path));

    public static string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static string FindRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Blocks.slnx")))
                return folder.FullName;
        }

        throw new InvalidOperationException("Blocks.slnx not found above " + AppContext.BaseDirectory);
    }
}
