namespace Blocks.Portability.Tests;

public sealed class SnapshotFixture : IDisposable
{
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", ".vscode", ".idea" };

    public SnapshotFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "blocks-portability", Guid.NewGuid().ToString("N"));
        Snapshot = Path.Combine(Root, "snapshot");
        Directory.CreateDirectory(Snapshot);

        CopyTree(Path.Combine(RepoRoot, "src"), Path.Combine(Snapshot, "src"));
        File.Copy(Path.Combine(RepoRoot, "Directory.Packages.props"), Path.Combine(Snapshot, "Directory.Packages.props"));
        Shell.Git(Snapshot, "init", "-q", "-b", "main");
        Shell.Git(Snapshot, "add", "-A");
        Shell.Git(Snapshot, "commit", "-q", "-m", "portability snapshot");
        Shell.Git(Snapshot, "remote", "add", "origin", Origin);
        Commit = Shell.Git(Snapshot, "rev-parse", "HEAD").Trim();
    }

    public static string RepoRoot { get; } = FindRoot();

    public static string Origin { get; } = Shell.Git(RepoRoot, "remote", "get-url", "origin").Trim();

    public string Root { get; }

    public string Snapshot { get; }

    public string Commit { get; }

    public static IEnumerable<string> Blocks()
        => Directory.EnumerateDirectories(Path.Combine(RepoRoot, "src"), "Blocks.*").Select(Path.GetFileName).Order(StringComparer.Ordinal)!;

    public void Dispose() => Shell.DeleteTree(Root);

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from))
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        foreach (var folder in Directory.EnumerateDirectories(from))
        {
            if (!SkippedFolders.Contains(Path.GetFileName(folder)))
                CopyTree(folder, Path.Combine(to, Path.GetFileName(folder)));
        }
    }

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
