using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Blocks.Sync;
using Xunit;

namespace Blocks.Portability.Tests;

[Trait("Category", "Portability")]
public sealed class PinReportTests(SnapshotFixture snapshot) : IClassFixture<SnapshotFixture>
{
    private static readonly string[] ProtobufPins = ["protobuf-net", "protobuf-net.Core"];

    [Fact]
    public void TheGrpcBlockAlone_IsGivenBothProtobufPins()
        => MissingPackages("Blocks.Grpc").Should().Contain(ProtobufPins);

    [Fact]
    public void TheAspNetCoreBlockAlone_IsGivenNeitherProtobufPin()
    {
        var missing = MissingPackages("Blocks.AspNetCore");

        missing.Should().NotBeEmpty();
        missing.Should().NotContain(ProtobufPins);
    }

    private List<string> MissingPackages(string block)
    {
        var app = Path.Combine(snapshot.Root, "pin-apps", block);
        Directory.CreateDirectory(app);
        Shell.Git(app, "init", "-q", "-b", "main");
        var manifest = new JsonObject
        {
            ["source"] = SnapshotFixture.Origin,
            ["commit"] = snapshot.Commit,
            ["blocksFolder"] = "blocks",
            ["packagesFile"] = "Directory.Packages.props",
            ["blocks"] = new JsonArray(block),
        };
        File.WriteAllText(Path.Combine(app, "blocks.json"), manifest.ToJsonString());

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        SyncCli.Run(["forward", "--app", app, "--source", snapshot.Snapshot, "--json"], stdout, stderr)
            .Should().Be(0, stderr.ToString());

        using var report = JsonDocument.Parse(stdout.ToString());
        return report.RootElement.GetProperty("missing").EnumerateArray().Select(p => p.GetProperty("id").GetString()!).ToList();
    }
}
