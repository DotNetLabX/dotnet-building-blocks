namespace Blocks.Sync;

internal static class StatusCommand
{
    public static int Run(SyncSession session, TextWriter info)
    {
        var closure = session.Closure();
        foreach (var block in closure.Union(session.Lock.Blocks.Keys).Order(StringComparer.Ordinal))
        {
            var state = State(session, block, closure.Contains(block));
            var head = BlockVersion.FromCommit(session.SourceRoot, "HEAD", block);
            if (!BlockVersion.Same(head.Files, session.AtCommit(block).Files))
                state += "; newer here";
            if (!BlockVersion.Same(BlockVersion.FromFolder(session.SourceFolder(block)).Files, head.Files))
                state += "; uncommitted here";
            info.WriteLine($"{block}: {state}");
        }

        return 0;
    }

    private static string State(SyncSession session, string block, bool listed)
    {
        var app = BlockVersion.FromFolder(session.AppFolder(block));
        if (!session.Lock.Blocks.TryGetValue(block, out var entry))
            return app.IsEmpty ? "not yet taken" : "not yet taken; the folder exists, so the first take needs --adopt";
        if (!listed)
            return "no longer listed";

        var other = session.AtCommit(block).Files;
        if (BlockVersion.Same(app.Files, other))
            return "in step";
        if (BlockVersion.Same(app.Files, entry.Files))
            return "changed here";
        return BlockVersion.Same(other, entry.Files) ? "changed in the app" : "changed on both sides";
    }
}
