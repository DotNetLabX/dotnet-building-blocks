using System.Text.Json.Nodes;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class CliTests : IDisposable
{
    private readonly SyncFixture _f = new();

    public void Dispose() => _f.Dispose();

    [Theory]
    [InlineData("pull")]
    [InlineData("forward", "--unknown")]
    [InlineData("back", "--adopt")]
    [InlineData("status", "--json")]
    public void UsageError_ExitsWithTwo(string command, params string[] extra)
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        var result = _f.Run(command, extra);

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().StartWith("usage: {forward|back|status} --app <path>");
    }

    [Fact]
    public void MissingApp_IsAUsageError()
    {
        var stderr = new StringWriter();

        SyncCli.Run(["status"], new StringWriter(), stderr).Should().Be(2);

        stderr.ToString().Should().StartWith("usage:");
    }

    [Fact]
    public void AppWithNoManifest_IsAnEnvironmentError()
    {
        var result = _f.Run("status");

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().Contain("no blocks.json in");
    }

    [Fact]
    public void ManifestMissingAField_IsAnEnvironmentError()
    {
        _f.WriteApp("blocks.json", """{ "source": "x", "commit": "y", "blocksFolder": "blocks", "blocks": [] }""");

        var result = _f.Run("status");

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().Contain("blocks.json needs a text value 'packagesFile'");
    }

    [Theory]
    [InlineData("--app")]
    [InlineData("--source")]
    public void EmptyPath_IsAUsageError(string option)
    {
        string[] args = option == "--app" ? ["status", "--app", ""] : ["status", "--app", _f.App, "--source", ""];

        var result = SyncFixture.RunArgs(args);

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().StartWith("usage:");
    }

    [Fact]
    public void PathWithAnInvalidCharacter_IsAUsageError()
    {
        var result = SyncFixture.RunArgs("status", "--app", "app\0folder");

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().StartWith("usage:");
    }

    [Fact]
    public void DefaultSource_IsTheCheckoutTheToolRunsFrom()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        var checkout = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(checkout.FullName, "Blocks.slnx")))
            checkout = checkout.Parent!;
        var origin = SyncFixture.Git(checkout.FullName, "remote", "get-url", "origin").Trim();

        var result = SyncFixture.RunArgs("status", "--app", _f.App);

        result.ExitCode.Should().Be(1, result.Output);
        result.Stderr.Should().Contain($"refused: this checkout's origin '{origin}' is not the manifest's source '{SyncFixture.Origin}'");
    }

    [Fact]
    public void DefaultSource_IsTheRootOfThatCheckout()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        var checkout = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(checkout.FullName, "Blocks.slnx")))
            checkout = checkout.Parent!;
        var manifestPath = Path.Combine(_f.App, "blocks.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
        manifest["source"] = SyncFixture.Git(checkout.FullName, "remote", "get-url", "origin").Trim();
        var missing = new string('0', 40);
        manifest["commit"] = missing;
        File.WriteAllText(manifestPath, manifest.ToJsonString());

        var result = SyncFixture.RunArgs("status", "--app", _f.App);

        result.ExitCode.Should().Be(2, result.Output);
        result.Stderr.Should().Contain($"commit {missing} is not in {checkout.FullName}; fetch first");
    }
}
