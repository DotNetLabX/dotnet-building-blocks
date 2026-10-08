using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed class PackageTests
{
    private static readonly HashSet<string> AllowedExpressions = new(StringComparer.OrdinalIgnoreCase)
    {
        "MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "MS-PL",
    };

    private static readonly HashSet<string> AllowedLicenceUrls = new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void PackageVersions_StayOnTheFreeReleases()
    {
        var versions = DeclaredVersions().ToList();

        versions.Where(v => v.Id.Equals("MediatR", StringComparison.OrdinalIgnoreCase)).Select(v => v.Version)
            .Should().NotBeEmpty().And.AllBe("12.5.0");

        var commercial = versions
            .Where(v => (v.Id.Equals("MediatR", StringComparison.OrdinalIgnoreCase) && Major(v.Version) >= 13)
                || (v.Id.Equals("AutoMapper", StringComparison.OrdinalIgnoreCase) && Major(v.Version) >= 15)
                || (v.Id.StartsWith("MassTransit", StringComparison.OrdinalIgnoreCase) && Major(v.Version) >= 9)
                || (v.Id.Equals("FluentAssertions", StringComparison.OrdinalIgnoreCase) && Major(v.Version) >= 8))
            .Select(v => $"{v.File}: {v.Id} {v.Version}");

        string.Join(Environment.NewLine, commercial).Should().BeEmpty();
    }

    [Fact]
    public void EveryResolvedPackage_CarriesAnAllowedLicence()
    {
        var packagesFolder = GlobalPackagesFolder();
        var refused = ResolvedPackages()
            .Select(p => (Package: p, Licence: Licence(Path.Combine(packagesFolder, p.Id.ToLowerInvariant(), p.Version.ToLowerInvariant(), $"{p.Id.ToLowerInvariant()}.nuspec"))))
            .Where(p => !Allowed(p.Licence))
            .Select(p => $"{p.Package.Id} {p.Package.Version}: {p.Licence}")
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, refused).Should().BeEmpty();
    }

    private static IEnumerable<(string File, string Id, string Version)> DeclaredVersions()
    {
        var files = RepoFiles.Under("", "*.csproj")
            .Concat(RepoFiles.Under("", "*.props"))
            .Concat(RepoFiles.Under("", "*.targets"));
        foreach (var file in files)
        {
            foreach (var item in XDocument.Parse(RepoFiles.Text(file)).Descendants())
            {
                var id = (string?)item.Attribute("Include") ?? (string?)item.Attribute("Update");
                var version = (string?)item.Attribute("Version") ?? (string?)item.Attribute("VersionOverride");
                if (id is not null && version is not null && item.Name.LocalName is "PackageVersion" or "PackageReference")
                    yield return (RepoFiles.Relative(file), id, version.Trim('[', ']', '(', ')', ' '));
            }
        }
    }

    private static int Major(string version)
        => int.TryParse(version.Split('.', ',')[0], out var major) ? major : int.MaxValue;

    private static bool Allowed(string licence)
    {
        if (licence.StartsWith("url ", StringComparison.Ordinal))
            return AllowedLicenceUrls.Contains(licence[4..]);
        if (!licence.StartsWith("expression ", StringComparison.Ordinal))
            return false;

        var alternatives = licence["expression ".Length..].Split(" OR ", StringSplitOptions.TrimEntries);
        return alternatives.Any(AllowedExpressions.Contains);
    }

    private static string Licence(string nuspec)
    {
        if (!File.Exists(nuspec))
            return $"no nuspec at {nuspec}";

        var metadata = XDocument.Load(nuspec).Descendants().First(e => e.Name.LocalName == "metadata");
        var licence = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "license");
        if (licence is not null)
            return (string?)licence.Attribute("type") == "expression" ? $"expression {licence.Value.Trim()}" : $"{(string?)licence.Attribute("type")} {licence.Value.Trim()}";

        var url = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "licenseUrl");
        return url is null ? "no licence" : $"url {url.Value.Trim()}";
    }

    private static List<(string Id, string Version)> ResolvedPackages()
    {
        using var report = JsonDocument.Parse(Dotnet("list", Path.Combine(RepoFiles.Root, "Blocks.slnx"), "package", "--include-transitive", "--format", "json"));
        return report.RootElement.GetProperty("projects").EnumerateArray()
            .Where(project => project.TryGetProperty("frameworks", out _))
            .SelectMany(project => project.GetProperty("frameworks").EnumerateArray())
            .SelectMany(framework => Packages(framework, "topLevelPackages").Concat(Packages(framework, "transitivePackages")))
            .Distinct()
            .ToList();
    }

    private static IEnumerable<(string Id, string Version)> Packages(JsonElement framework, string kind)
        => framework.TryGetProperty(kind, out var packages)
            ? packages.EnumerateArray().Select(p => (p.GetProperty("id").GetString()!, p.GetProperty("resolvedVersion").GetString()!))
            : [];

    private static string GlobalPackagesFolder()
    {
        var configured = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var line = Dotnet("nuget", "locals", "global-packages", "--list").Trim();
        return line[(line.IndexOf(':') + 1)..].Trim();
    }

    private static string Dotnet(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = RepoFiles.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);
        foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase) || k.StartsWith("VSTEST", StringComparison.OrdinalIgnoreCase)).ToList())
            start.Environment.Remove(key);

        using var process = Process.Start(start)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet {string.Join(' ', args)}: {stdout}{stderr.Result}");
        return stdout;
    }
}
