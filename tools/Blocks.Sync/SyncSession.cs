using System.Xml.Linq;

namespace Blocks.Sync;

internal sealed class SyncSession
{
    private SyncSession(string appRoot, string sourceRoot, Manifest manifest, string commit, LockFile lockFile)
    {
        AppRoot = appRoot;
        SourceRoot = sourceRoot;
        Manifest = manifest;
        Commit = commit;
        Lock = lockFile;
    }

    public string AppRoot { get; }
    public string SourceRoot { get; }
    public Manifest Manifest { get; }
    public string Commit { get; }
    public LockFile Lock { get; }

    public static SyncSession Open(string appRoot, string sourceRoot)
    {
        if (!Directory.Exists(appRoot))
            throw new EnvironmentException($"the app folder {appRoot} does not exist");
        if (!Git.TryText(sourceRoot, out _, "rev-parse", "--git-dir"))
            throw new EnvironmentException($"{sourceRoot} is not a git checkout");

        var manifest = Manifest.Load(appRoot);
        CheckOrigin(sourceRoot, manifest.Source);
        var commit = Resolve(sourceRoot, manifest.Commit);
        var lockFile = LockFile.Load(appRoot, manifest.BlocksFolder);
        var lockCommits = lockFile.Blocks.ToDictionary(b => b.Key, b => Resolve(sourceRoot, b.Value.Commit));
        foreach (var (block, entry) in lockFile.Blocks)
        {
            var atCommit = BlockVersion.FromCommit(sourceRoot, lockCommits[block], block).Files;
            if (!BlockVersion.Same(atCommit, entry.Files))
                throw new RefusedException(Messages.LockIsNotTheBlock(block, entry.Commit, atCommit, entry.Files));
        }

        return new SyncSession(appRoot, sourceRoot, manifest, commit, lockFile);
    }

    public string AppFolder(string block) => Path.Combine(AppRoot, Manifest.BlocksFolder, block);

    public string SourceFolder(string block) => Path.Combine(SourceRoot, "src", block);

    public bool ExistsAt(string commit, string block)
        => Git.TryText(SourceRoot, out _, "cat-file", "-e", $"{commit}:src/{block}/{block}.csproj");

    public BlockVersion AtCommit(string block) => BlockVersion.FromCommit(SourceRoot, Commit, block);

    public SortedSet<string> Closure()
    {
        var closure = new SortedSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>(Manifest.Blocks);
        while (pending.Count > 0)
        {
            var block = pending.Dequeue();
            if (!closure.Add(block))
                continue;

            foreach (var reference in ProjectReferences(block))
                pending.Enqueue(reference);
        }

        return closure;
    }

    public XDocument ProjectAtCommit(string block)
    {
        if (!ExistsAt(Commit, block))
            throw new RefusedException($"refused: {block} does not exist under src/ at {Commit}");
        return XDocument.Parse(Git.Text(SourceRoot, "cat-file", "blob", $"{Commit}:src/{block}/{block}.csproj"));
    }

    private IEnumerable<string> ProjectReferences(string block)
    {
        foreach (var reference in ProjectAtCommit(block).Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
        {
            var include = (string?)reference.Attribute("Include") ?? "";
            var name = Path.GetFileNameWithoutExtension(include.Replace('\\', '/').Split('/')[^1]);
            Containment.CheckBlockName(name, $"{block}'s project references");
            yield return name;
        }
    }

    private static void CheckOrigin(string sourceRoot, string expected)
    {
        Git.TryText(sourceRoot, out var origin, "remote", "get-url", "origin");
        if (!string.Equals(Normalize(origin), Normalize(expected), StringComparison.OrdinalIgnoreCase))
            throw new RefusedException($"refused: this checkout's origin '{origin.Trim()}' is not the manifest's source '{expected}'");
    }

    private static string Normalize(string url)
    {
        var trimmed = url.Trim().TrimEnd('/');
        return trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? trimmed[..^4] : trimmed;
    }

    private static string Resolve(string sourceRoot, string commit)
    {
        if (!Git.TryText(sourceRoot, out var sha, "rev-parse", "--verify", "--quiet", $"{commit}^{{commit}}"))
            throw new EnvironmentException($"commit {commit} is not in {sourceRoot}; fetch first");
        return sha.Trim();
    }
}
