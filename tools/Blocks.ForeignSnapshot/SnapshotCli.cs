using System.Text;

namespace Blocks.ForeignSnapshot;

public static class SnapshotCli
{
    private const string Usage = "usage: record --repo <path> [--repo <path> ...] --out <file> | compare --before <file> --after <file>";

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0 || args[0] is not ("record" or "compare") || !TryReadOptions(args, out var options))
        {
            stderr.WriteLine(Usage);
            return 2;
        }

        return args[0] == "record" ? Record(options, stdout, stderr) : Compare(options, stdout, stderr);
    }

    private static bool TryReadOptions(string[] args, out List<(string Name, string Value)> options)
    {
        options = [];
        for (var i = 1; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal))
                return false;
            options.Add((args[i], Path.GetFullPath(args[i + 1])));
        }

        return true;
    }

    private static int Record(List<(string Name, string Value)> options, TextWriter stdout, TextWriter stderr)
    {
        var repos = options.Where(o => o.Name == "--repo").Select(o => o.Value).ToList();
        var output = options.LastOrDefault(o => o.Name == "--out").Value;
        if (repos.Count == 0 || output is null || options.Any(o => o.Name is not ("--repo" or "--out")))
        {
            stderr.WriteLine(Usage);
            return 2;
        }

        string record;
        try
        {
            record = SnapshotRecorder.Record(repos);
        }
        catch (GitException ex)
        {
            stderr.WriteLine(ex.Message);
            return 2;
        }

        File.WriteAllText(output, record, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        stdout.WriteLine($"recorded {repos.Count} repositories to {output}");
        return 0;
    }

    private static int Compare(List<(string Name, string Value)> options, TextWriter stdout, TextWriter stderr)
    {
        var before = options.LastOrDefault(o => o.Name == "--before").Value;
        var after = options.LastOrDefault(o => o.Name == "--after").Value;
        if (before is null || after is null || options.Count != 2)
        {
            stderr.WriteLine(Usage);
            return 2;
        }

        var comparison = SnapshotComparer.Compare(File.ReadAllText(before), File.ReadAllText(after));
        stdout.WriteLine($"must be equal: {comparison.MustBeEqual.Count} difference(s)");
        foreach (var line in comparison.MustBeEqual)
            stdout.WriteLine($"  {line}");
        stdout.WriteLine($"listed, not failed: {comparison.Listed.Count} difference(s)");
        foreach (var line in comparison.Listed)
            stdout.WriteLine($"  {line}");
        return comparison.MustBeEqual.Count == 0 ? 0 : 1;
    }
}
