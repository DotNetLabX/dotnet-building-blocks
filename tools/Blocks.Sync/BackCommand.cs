namespace Blocks.Sync;

internal static class BackCommand
{
    private sealed record Copy(string Block, BlockVersion Current, BlockVersion Wanted);

    public static int Run(SyncSession session, TextWriter info, TextWriter errors)
    {
        var refusals = new List<string>();
        var copies = new List<Copy>();

        foreach (var (block, entry) in session.Lock.Blocks)
        {
            var app = BlockVersion.FromFolder(session.AppFolder(block));
            if (BlockVersion.Same(app.Files, entry.Files))
                continue;

            var folder = session.SourceFolder(block);
            if (!File.Exists(Path.Combine(folder, $"{block}.csproj")))
                throw new RefusedException($"refused: {block} does not exist under src/ in {session.SourceRoot}");

            var here = BlockVersion.FromFolder(folder);
            if (BlockVersion.Same(here.Files, app.Files))
                info.WriteLine($"{block}: in step");
            else if (!BlockVersion.Same(here.Files, entry.Files))
                refusals.Add(Messages.ChangedOnBothSides(block, entry.Files, app.Files, here.Files));
            else
                copies.Add(new Copy(block, here, app));
        }

        if (refusals.Count > 0)
        {
            foreach (var refusal in refusals)
                errors.WriteLine(refusal);
            return 1;
        }

        foreach (var copy in copies)
        {
            Containment.CheckNoLinks(session.SourceRoot, session.SourceFolder(copy.Block), copy.Block);
            SafeFiles.CheckReplaceable(session.SourceFolder(copy.Block), copy.Current, copy.Wanted, copy.Block);
        }

        foreach (var copy in copies)
        {
            SafeFiles.Mirror(session.SourceFolder(copy.Block), copy.Current, copy.Wanted, BlockVersion.WithoutByteOrderMark);
            info.WriteLine($"{copy.Block}: sent back to {session.SourceFolder(copy.Block)}; commit it there and set the app's manifest to that commit");
        }

        return 0;
    }
}
