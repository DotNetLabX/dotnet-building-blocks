using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Blocks.Sync;

internal sealed record PackageReport(List<(string Id, string Version)> Missing, List<(string Id, string Ours, string Theirs)> Different)
{
    private const string CentralFile = "Directory.Packages.props";

    public static PackageReport Build(SyncSession session, IReadOnlyCollection<string> blocks)
    {
        if (!Git.TryText(session.SourceRoot, out var centralText, "cat-file", "blob", $"{session.Commit}:{CentralFile}"))
            throw new EnvironmentException($"{session.SourceRoot} has no {CentralFile} at {session.Commit}");

        var central = Parse(centralText, CentralFile).Descendants().Where(e => e.Name.LocalName == "PackageVersion").ToList();
        var needed = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var block in blocks)
        {
            foreach (var reference in session.ProjectAtCommit(block).Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                var id = (string?)reference.Attribute("Include");
                if (id is null)
                    continue;
                needed[id] = (string?)reference.Attribute("Version")
                    ?? (string?)central.FirstOrDefault(p => string.Equals((string?)p.Attribute("Include"), id, StringComparison.OrdinalIgnoreCase))?.Attribute("Version")
                    ?? "";
            }
        }

        foreach (var pin in central)
        {
            var pinnedFor = ((string?)pin.Attribute("Blocks") ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (pinnedFor.Any(blocks.Contains) && (string?)pin.Attribute("Include") is { } id)
                needed[id] = (string?)pin.Attribute("Version") ?? "";
        }

        var app = AppVersions(Path.Combine(session.AppRoot, session.Manifest.PackagesFile), session.Manifest.PackagesFile);
        var report = new PackageReport([], []);
        foreach (var (id, version) in needed)
        {
            if (!app.TryGetValue(id, out var theirs))
                report.Missing.Add((id, version));
            else if (theirs != version)
                report.Different.Add((id, version, theirs));
        }

        return report;
    }

    public void WriteText(TextWriter output, string packagesFile)
    {
        if (Missing.Count == 0 && Different.Count == 0)
        {
            output.WriteLine($"packages: the app's {packagesFile} has every version these blocks need");
            return;
        }

        output.WriteLine($"packages the app's {packagesFile} lacks or has at another version:");
        var lines = Missing.Select(m => (m.Id, Text: $"missing {m.Id} {m.Version}"))
            .Concat(Different.Select(d => (d.Id, Text: $"different {d.Id}: here {d.Ours}, app {d.Theirs}")))
            .OrderBy(l => l.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
            output.WriteLine($"  {line.Text}");
    }

    public void WriteJson(TextWriter output)
    {
        var json = new
        {
            missing = Missing.Select(m => new { id = m.Id, version = m.Version }),
            different = Different.Select(d => new { id = d.Id, ours = d.Ours, theirs = d.Theirs }),
        };
        output.WriteLine(JsonSerializer.Serialize(json));
    }

    private static Dictionary<string, string> AppVersions(string path, string name)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
            return versions;

        foreach (var package in Parse(File.ReadAllText(path), name).Descendants().Where(e => e.Name.LocalName == "PackageVersion"))
        {
            if ((string?)package.Attribute("Include") is { } id)
                versions[id] = (string?)package.Attribute("Version") ?? "";
        }

        return versions;
    }

    private static XDocument Parse(string text, string name)
    {
        try
        {
            return XDocument.Parse(text);
        }
        catch (XmlException ex)
        {
            throw new EnvironmentException($"{name} cannot be read: {ex.Message}");
        }
    }
}
