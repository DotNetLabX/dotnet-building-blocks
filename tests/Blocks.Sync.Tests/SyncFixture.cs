using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AwesomeAssertions;

namespace Blocks.Sync.Tests;

internal sealed record SyncResult(int ExitCode, string Stdout, string Stderr)
{
    public string Output => Stdout + Stderr;
}

internal sealed class SyncFixture : IDisposable
{
    public const string Origin = "https://example.test/blocks.git";
    public const string BlocksFolder = "blocks";

    public const string WayOut =
        "way out: set the app's edit aside, set the app's manifest to the commit here that holds the other change, take the block forward, apply the edit again, then send it back";

    private const string PackagesProps = """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
          </PropertyGroup>
          <ItemGroup>
            <PackageVersion Include="Humanizer.Core" Version="2.14.1" />
            <PackageVersion Include="MediatR.Contracts" Version="2.0.1" />
            <PackageVersion Include="FastEndpoints" Version="8.3.0" />
            <PackageVersion Include="protobuf-net" Version="3.2.56" Blocks="Blocks.Web" />
            <PackageVersion Include="xunit.v3" Version="3.2.2" />
          </ItemGroup>
        </Project>
        """;

    public SyncFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "blocks-sync-tests", Guid.NewGuid().ToString("N"));
        Source = Path.Combine(Root, "blocks-repo");
        App = Path.Combine(Root, "app");
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(App);

        Git(Source, "init", "-q", "-b", "main");
        Git(Source, "remote", "add", "origin", Origin);
        Git(App, "init", "-q", "-b", "main");

        WriteSource("Directory.Packages.props", PackagesProps);
        WriteSource("src/Blocks.Core/Blocks.Core.csproj", Project(["Humanizer.Core"], []));
        WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n\npublic static class Text;\n");
        WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n\npublic static class Casing;\n");
        WriteSource("src/Blocks.Domain/Blocks.Domain.csproj", Project(["MediatR.Contracts"], ["Blocks.Core"]));
        WriteSource("src/Blocks.Domain/Entity.cs", "namespace Blocks.Domain;\n\npublic abstract class Entity;\n");
        WriteSource("src/Blocks.Web/Blocks.Web.csproj", Project(["FastEndpoints"], ["Blocks.Domain"]));
        WriteSource("src/Blocks.Web/Endpoint.cs", "namespace Blocks.Web;\n\npublic abstract class Endpoint;\n");
        WriteSource("tests/Blocks.Core.Tests/TextTests.cs", "namespace Blocks.Core.Tests;\n");
        InitialCommit = CommitSource("initial");
    }

    public string Root { get; }
    public string Source { get; }
    public string App { get; }
    public string InitialCommit { get; }

    public string LockPath => Path.Combine(App, "blocks.lock.json");

    public static string Project(string[] packages, string[] references)
    {
        var text = new StringBuilder("<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n  <ItemGroup>\n");
        foreach (var package in packages)
            text.Append($"    <PackageReference Include=\"{package}\" />\n");
        foreach (var reference in references)
            text.Append($"    <ProjectReference Include=\"..\\{reference}\\{reference}.csproj\" />\n");
        return text.Append("  </ItemGroup>\n</Project>\n").ToString();
    }

    public void WriteSource(string relative, string text) => WriteBytes(Path.Combine(Source, relative), Encoding.UTF8.GetBytes(text));

    public void WriteApp(string relative, string text) => WriteBytes(Path.Combine(App, relative), Encoding.UTF8.GetBytes(text));

    public void WriteAppBlock(string block, string file, string text) => WriteApp($"{BlocksFolder}/{block}/{file}", text);

    public static void WriteBytes(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    public string AppBlockPath(string block, string file = "") => Path.Combine(App, BlocksFolder, block, file);

    public string ReadAppBlock(string block, string file) => File.ReadAllText(AppBlockPath(block, file));

    public string ReadSource(string relative) => File.ReadAllText(Path.Combine(Source, relative));

    public string CommitSource(string message)
    {
        Git(Source, "add", "-A");
        Git(Source, "commit", "-q", "--allow-empty", "-m", message);
        return Git(Source, "rev-parse", "HEAD").Trim();
    }

    public void CommitApp(string message)
    {
        Git(App, "add", "-A");
        Git(App, "commit", "-q", "--allow-empty", "-m", message);
    }

    public void WriteManifest(string commit, params string[] blocks)
        => WriteManifestIn(BlocksFolder, commit, blocks);

    public void WriteManifestIn(string blocksFolder, string commit, params string[] blocks)
    {
        var manifest = new JsonObject
        {
            ["source"] = Origin,
            ["commit"] = commit,
            ["blocksFolder"] = blocksFolder,
            ["packagesFile"] = "Directory.Packages.props",
            ["blocks"] = new JsonArray(blocks.Select(b => (JsonNode)b).ToArray()),
        };
        WriteApp("blocks.json", manifest.ToJsonString());
    }

    public SyncResult Run(string command, params string[] extra) => RunIn(App, Source, command, extra);

    public static SyncResult RunIn(string app, string source, string command, params string[] extra)
        => RunArgs([command, "--app", app, "--source", source, .. extra]);

    public static SyncResult RunArgs(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = SyncCli.Run(args, stdout, stderr);
        return new SyncResult(exitCode, stdout.ToString().ReplaceLineEndings("\n"), stderr.ToString().ReplaceLineEndings("\n"));
    }

    public SyncResult Forward(params string[] extra) => Succeeds("forward", extra);

    public SyncResult Back() => Succeeds("back");

    public SyncResult Status() => Succeeds("status");

    public SyncResult Refused(string command, params string[] extra)
    {
        var result = Run(command, extra);
        result.ExitCode.Should().Be(1, result.Output);
        return result;
    }

    private SyncResult Succeeds(string command, params string[] extra)
    {
        var result = Run(command, extra);
        result.ExitCode.Should().Be(0, result.Output);
        return result;
    }

    public JsonNode ReadLock() => JsonNode.Parse(File.ReadAllText(LockPath))!;

    public SortedDictionary<string, string> Listing(params string[] allowedPaths)
    {
        var allowed = allowedPaths.Select(p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar)).ToArray();
        var listing = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint };
        foreach (var file in Directory.EnumerateFiles(Root, "*", options))
        {
            if (allowed.Any(a => file.Equals(a, StringComparison.OrdinalIgnoreCase)
                || file.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                continue;
            listing[Path.GetRelativePath(Root, file)] = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        }

        return listing;
    }

    public static string Git(string repo, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var setting in new[] { "core.autocrlf=false", "user.name=Sync Test", "user.email=sync@example.test", "commit.gpgsign=false" })
        {
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add(setting);
        }

        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(repo);
        foreach (var arg in args)
            start.ArgumentList.Add(arg);

        using var process = Process.Start(start)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)}: {stderr.Result}");
        return stdout;
    }

    public static void CreateJunction(string link, string target)
    {
        if (OperatingSystem.IsWindows())
            Shell("cmd.exe", "/c", "mklink", "/J", link, target);
        else
            Directory.CreateSymbolicLink(link, target);
    }

    public static void CreateHardLink(string link, string target)
    {
        if (OperatingSystem.IsWindows())
            Shell("cmd.exe", "/c", "mklink", "/H", link, target);
        else
            Shell("ln", target, link);
    }

    private static void Shell(string program, params string[] args)
    {
        var start = new ProcessStartInfo(program) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var stderr = process.StandardError.ReadToEndAsync();
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{program} {string.Join(' ', args)}: {stderr.Result}");
    }

    public void Dispose()
    {
        if (!Directory.Exists(Root))
            return;

        var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint };
        var folders = Directory.EnumerateDirectories(Root, "*", options).Prepend(Root);
        var links = folders.SelectMany(Directory.EnumerateDirectories)
            .Where(d => new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.ReparsePoint))
            .ToList();
        foreach (var link in links)
            Directory.Delete(link);
        foreach (var file in Directory.EnumerateFiles(Root, "*", options))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(Root, recursive: true);
    }
}
