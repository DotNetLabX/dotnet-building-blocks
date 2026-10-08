namespace Blocks.Sync;

internal static class FirstTake
{
    public static List<string> NotCommitted(string folder)
    {
        string prefix;
        string status;
        try
        {
            prefix = Git.Text(folder, "rev-parse", "--show-prefix").Trim();
            status = Git.Text(folder, "status", "--porcelain", "-z", "--ignored", "--", ".");
        }
        catch (GitException ex)
        {
            return [$"the app's folder is not in a git repository ({ex.Message})"];
        }

        var found = new List<string>();
        var records = status.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < records.Length; i++)
        {
            var record = records[i];
            if (record[0] is 'R' or 'C')
                i++;

            var path = record[3..];
            if (Counts(prefix, path))
                found.Add(record);
        }

        return found.Count > 0 ? found : DifferFromLastCommit(folder);
    }

    private static List<string> DifferFromLastCommit(string folder)
    {
        string listing;
        try
        {
            listing = Git.Text(folder, "ls-tree", "-r", "-z", "HEAD", "--", ".");
        }
        catch (GitException ex)
        {
            return [$"the app's repository has no commit holding this folder ({ex.Message})"];
        }

        var committed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var record in listing.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = record.IndexOf('\t');
            var fields = record[..tab].Split(' ');
            if (fields[1] == "blob")
                committed[record[(tab + 1)..]] = fields[2];
        }

        var physical = BlockVersion.FromFolder(folder);
        return physical.Files
            .Where(file => !committed.TryGetValue(file.Key, out var blob)
                           || BlockVersion.Fingerprint(Git.Bytes(folder, "cat-file", "blob", blob)) != file.Value)
            .Select(file => $"differs from the app's last commit: {file.Key}")
            .ToList();
    }

    private static bool Counts(string prefix, string path)
    {
        var relative = path.StartsWith(prefix, StringComparison.Ordinal) ? path[prefix.Length..] : "";
        return BlockVersion.IsInFileSet(relative);
    }
}
