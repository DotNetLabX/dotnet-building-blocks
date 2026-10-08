using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Blocks.Sync;
using Xunit;

namespace Blocks.Portability.Tests;

[Trait("Category", "Portability")]
public sealed class PortabilityTests(SnapshotFixture snapshot) : IClassFixture<SnapshotFixture>
{
    public static TheoryData<string> Blocks() => new(SnapshotFixture.Blocks());

    [Theory]
    [MemberData(nameof(Blocks))]
    public void Block_TakenAloneIntoAnEmptyApp_Builds(string block)
    {
        var app = Path.Combine(snapshot.Root, "apps", block);
        Path.GetFullPath(app).Should().NotStartWith(@"D:\src", "every temporary path is outside the source folders");
        Path.GetFullPath(snapshot.Snapshot).Should().NotStartWith(@"D:\src");
        Directory.CreateDirectory(app);
        Shell.Git(app, "init", "-q", "-b", "main");
        WriteManifest(app, block);

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        SyncCli.Run(["forward", "--app", app, "--source", snapshot.Snapshot, "--json"], stdout, stderr)
            .Should().Be(0, stderr.ToString());

        using var report = JsonDocument.Parse(stdout.ToString());
        report.RootElement.GetProperty("different").GetArrayLength().Should().Be(0);
        File.WriteAllText(Path.Combine(app, "Directory.Packages.props"), CentralFile(report.RootElement.GetProperty("missing")));

        var taken = JsonNode.Parse(File.ReadAllText(Path.Combine(app, "blocks.lock.json")))!["blocks"]!.AsObject().Select(b => b.Key).ToList();
        taken.Should().Contain(block);
        foreach (var copied in taken)
            FilesOf(Path.Combine(app, "blocks", copied)).Should().Equal(FilesOf(Path.Combine(snapshot.Snapshot, "src", copied)), $"{copied} is copied whole");

        var probe = Path.Combine(app, "Probe");
        Directory.CreateDirectory(probe);
        File.WriteAllText(Path.Combine(probe, "Probe.csproj"), ProbeProject(block));
        File.WriteAllText(Path.Combine(probe, "Probe.cs"), "namespace Probe;\n\npublic static class Marker;\n");

        var build = Shell.Run("dotnet", probe, "build", "Probe.csproj", "-nologo", "-v:minimal");

        build.ExitCode.Should().Be(0, build.Output);
    }

    private void WriteManifest(string app, string block)
    {
        var manifest = new JsonObject
        {
            ["source"] = SnapshotFixture.Origin,
            ["commit"] = snapshot.Commit,
            ["blocksFolder"] = "blocks",
            ["packagesFile"] = "Directory.Packages.props",
            ["blocks"] = new JsonArray(block),
        };
        File.WriteAllText(Path.Combine(app, "blocks.json"), manifest.ToJsonString());
    }

    private static string CentralFile(JsonElement missing)
    {
        var text = new StringBuilder("<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n");
        text.Append("    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>\n  </PropertyGroup>\n  <ItemGroup>\n");
        foreach (var package in missing.EnumerateArray())
            text.Append($"    <PackageVersion Include=\"{package.GetProperty("id").GetString()}\" Version=\"{package.GetProperty("version").GetString()}\" />\n");
        return text.Append("  </ItemGroup>\n</Project>\n").ToString();
    }

    private static string ProbeProject(string block) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\blocks\{block}\{block}.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static List<string> FilesOf(string folder)
        => Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(f => $"{Path.GetRelativePath(folder, f).Replace('\\', '/')} {Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f)))}")
            .Order(StringComparer.Ordinal)
            .ToList();
}
