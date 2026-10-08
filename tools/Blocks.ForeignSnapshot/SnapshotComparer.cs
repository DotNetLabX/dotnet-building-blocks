namespace Blocks.ForeignSnapshot;

public sealed record SnapshotComparison(IReadOnlyList<string> MustBeEqual, IReadOnlyList<string> Listed);

public static class SnapshotComparer
{
    public const string StrictFolder = "src/BuildingBlocks/";

    private const string RepoHeader = "# repo\t";

    public static SnapshotComparison Compare(string before, string after)
    {
        var beforeSections = Sections(before);
        var afterSections = Sections(after);
        var strictRepo = beforeSections.Count > 0 ? beforeSections[0].Repo : null;
        var mustBeEqual = new List<string>();
        var listed = new List<string>();

        foreach (var repo in beforeSections.Select(s => s.Repo).Union(afterSections.Select(s => s.Repo)))
        {
            var was = beforeSections.Where(s => s.Repo == repo).Select(s => s.Lines).FirstOrDefault();
            var now = afterSections.Where(s => s.Repo == repo).Select(s => s.Lines).FirstOrDefault();
            if (was is null || now is null)
            {
                mustBeEqual.Add($"{repo}: recorded {(was is null ? "after" : "before")} only");
                continue;
            }

            var differences = was.Except(now).Select(l => (Sign: "-", Line: l))
                .Concat(now.Except(was).Select(l => (Sign: "+", Line: l)))
                .OrderBy(d => d.Line, StringComparer.Ordinal);
            foreach (var (sign, line) in differences)
            {
                var difference = $"{sign} {repo}\t{line}";
                if (repo == strictRepo || IsStrict(line))
                    mustBeEqual.Add(difference);
                else
                    listed.Add(difference);
            }
        }

        return new SnapshotComparison(mustBeEqual, listed);
    }

    private static bool IsStrict(string line)
    {
        var tab = line.IndexOf('\t');
        var kind = line[..tab];
        var rest = line[(tab + 1)..];
        if (kind == "head")
            return true;

        var paths = kind == "status" ? rest[3..].Split(" -> ") : [rest.Split('\t')[0]];
        return paths.Any(InStrictFolder);
    }

    private static bool InStrictFolder(string path)
    {
        var normalized = path.Trim('"').Replace('\\', '/');
        return normalized.StartsWith(StrictFolder, StringComparison.OrdinalIgnoreCase)
            || (normalized.EndsWith('/') && StrictFolder.StartsWith(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static List<(string Repo, HashSet<string> Lines)> Sections(string record)
    {
        var sections = new List<(string Repo, HashSet<string> Lines)>();
        foreach (var line in record.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith(RepoHeader, StringComparison.Ordinal))
                sections.Add((line[RepoHeader.Length..], new HashSet<string>(StringComparer.Ordinal)));
            else if (sections.Count > 0)
                sections[^1].Lines.Add(line);
            else
                throw new FormatException("a record line comes before the first repo header");
        }

        return sections;
    }
}
