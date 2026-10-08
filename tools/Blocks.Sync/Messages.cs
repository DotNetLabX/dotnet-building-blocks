using System.Text;

namespace Blocks.Sync;

internal static class Messages
{
    public const string WayOut =
        "way out: set the app's edit aside, set the app's manifest to the commit here that holds the other change, "
        + "take the block forward, apply the edit again, then send it back";

    public static string ChangedInApp(string block, IReadOnlyDictionary<string, string> lockFiles, IReadOnlyDictionary<string, string> app)
        => Refusal($"{block} changed in the app; send it back first", BlockVersion.Changes(lockFiles, app));

    public static string ChangedOnBothSides(
        string block,
        IReadOnlyDictionary<string, string> lockFiles,
        IReadOnlyDictionary<string, string> app,
        IReadOnlyDictionary<string, string> here)
    {
        var text = new StringBuilder($"refused: {block} changed on both sides\n");
        text.Append("  in the app:\n");
        foreach (var change in BlockVersion.Changes(lockFiles, app))
            text.Append($"    {change}\n");
        text.Append("  here:\n");
        foreach (var change in BlockVersion.Changes(lockFiles, here))
            text.Append($"    {change}\n");
        return text.Append($"  {WayOut}").ToString();
    }

    public static string FirstTake(string block, IReadOnlyDictionary<string, string> app, IReadOnlyDictionary<string, string> wanted)
        => Refusal($"{block} already has a folder in the app but no lock entry; run forward with --adopt to replace it", BlockVersion.Changes(app, wanted));

    public static string NotCommitted(string block, IEnumerable<string> entries)
        => Refusal($"{block}: the app's folder holds files its repository has not committed (changed, untracked or ignored); they could not be recovered", entries);

    public static string FolderInTheWay(string block, string path, IEnumerable<string> kept)
        => Refusal($"{block}: {path} cannot replace the folder of that name; it holds files outside the block's files", kept);

    public static string FileInTheWay(string block, string path, string file)
        => Refusal($"{block}: {path} needs the folder {file}, but a file outside the block's files has that name", [file]);

    public static string LockIsNotTheBlock(
        string block,
        string commit,
        IReadOnlyDictionary<string, string> atCommit,
        IReadOnlyDictionary<string, string> lockFiles)
        => Refusal($"{LockFile.FileName} cannot be read: {block}'s files are not the block's files at {commit}", BlockVersion.Changes(atCommit, lockFiles));

    private static string Refusal(string headline, IEnumerable<string> files)
    {
        var text = new StringBuilder($"refused: {headline}");
        foreach (var file in files)
            text.Append($"\n  {file}");
        return text.ToString();
    }
}
