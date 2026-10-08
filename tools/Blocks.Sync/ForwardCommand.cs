namespace Blocks.Sync;

internal static class ForwardCommand
{
    private sealed record Copy(string Block, BlockVersion Current, BlockVersion Wanted);

    public static int Run(SyncSession session, bool adopt, bool json, TextWriter output, TextWriter errors)
    {
        var info = json ? errors : output;
        var closure = session.Closure();
        var refusals = new List<string>();
        var copies = new List<Copy>();
        var newLock = new LockFile();

        foreach (var block in closure.Union(session.Lock.Blocks.Keys).Order(StringComparer.Ordinal))
        {
            var app = BlockVersion.FromFolder(session.AppFolder(block));
            if (session.Lock.Blocks.TryGetValue(block, out var entry))
            {
                var appChanged = !BlockVersion.Same(app.Files, entry.Files);
                if (!closure.Contains(block))
                {
                    newLock.Blocks[block] = entry;
                    if (appChanged)
                        refusals.Add(Messages.ChangedInApp(block, entry.Files, app.Files));
                    continue;
                }

                var other = session.AtCommit(block);
                newLock.Blocks[block] = new LockEntry(session.Commit, other.Files);
                if (BlockVersion.Same(app.Files, other.Files))
                {
                    info.WriteLine($"{block}: in step");
                    continue;
                }

                if (appChanged && !BlockVersion.Same(other.Files, entry.Files))
                    refusals.Add(Messages.ChangedOnBothSides(block, entry.Files, app.Files, other.Files));
                else if (appChanged)
                    refusals.Add(Messages.ChangedInApp(block, entry.Files, app.Files));
                else
                    copies.Add(new Copy(block, app, other));
                continue;
            }

            var wanted = session.AtCommit(block);
            newLock.Blocks[block] = new LockEntry(session.Commit, wanted.Files);
            if (app.IsEmpty)
            {
                copies.Add(new Copy(block, app, wanted));
                continue;
            }

            if (!adopt)
            {
                refusals.Add(Messages.FirstTake(block, app.Files, wanted.Files));
                continue;
            }

            var notCommitted = FirstTake.NotCommitted(session.AppFolder(block));
            if (notCommitted.Count > 0)
            {
                refusals.Add(Messages.NotCommitted(block, notCommitted));
                continue;
            }

            copies.Add(new Copy(block, app, wanted));
        }

        if (refusals.Count > 0)
        {
            foreach (var refusal in refusals)
                errors.WriteLine(refusal);
            return 1;
        }

        var report = PackageReport.Build(session, closure);

        Containment.CheckNoLinks(session.AppRoot, session.AppRoot, LockFile.FileName);
        foreach (var copy in copies)
        {
            Containment.CheckNoLinks(session.AppRoot, session.AppFolder(copy.Block), copy.Block);
            SafeFiles.CheckReplaceable(session.AppFolder(copy.Block), copy.Current, copy.Wanted, copy.Block);
        }

        foreach (var copy in copies)
        {
            var removed = copy.Current.Files.Keys.Where(p => !copy.Wanted.Files.ContainsKey(p)).ToList();
            if (!session.Lock.Blocks.ContainsKey(copy.Block) && !copy.Current.IsEmpty)
            {
                info.WriteLine($"{copy.Block}: first take; removing {removed.Count} file(s) from the app's folder:");
                foreach (var path in removed)
                    info.WriteLine($"  {path}");
            }

            SafeFiles.Mirror(session.AppFolder(copy.Block), copy.Current, copy.Wanted, content => content);
            info.WriteLine($"{copy.Block}: copied at {session.Commit}");
        }

        SafeFiles.Write(Path.Combine(session.AppRoot, LockFile.FileName), newLock.ToBytes());

        if (json)
            report.WriteJson(output);
        else
            report.WriteText(output, session.Manifest.PackagesFile);
        return 0;
    }
}
