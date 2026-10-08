using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class PackageReportTests : IDisposable
{
    private const string AppPackages = """
        <Project>
          <ItemGroup>
            <PackageVersion Include="Humanizer.Core" Version="2.14.1" />
            <PackageVersion Include="MediatR.Contracts" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

    private readonly SyncFixture _f = new();

    public void Dispose() => _f.Dispose();

    [Fact]
    public void Flow1_PackageReport_ListsMissingAndDifferentVersionsIncludingAnAnnotatedPin()
    {
        _f.WriteApp("Directory.Packages.props", AppPackages);
        _f.WriteManifest(_f.InitialCommit, "Blocks.Web");

        var result = _f.Forward();

        result.Stdout.Should().EndWith(
            "packages the app's Directory.Packages.props lacks or has at another version:\n"
            + "  missing FastEndpoints 8.3.0\n"
            + "  different MediatR.Contracts: here 2.0.1, app 1.0.0\n"
            + "  missing protobuf-net 3.2.56\n");
    }

    [Fact]
    public void Flow1_PackageReport_AsJson_IsOneParsableObjectOnStdout()
    {
        _f.WriteApp("Directory.Packages.props", AppPackages);
        _f.WriteManifest(_f.InitialCommit, "Blocks.Web");

        var result = _f.Forward("--json");

        using var report = JsonDocument.Parse(result.Stdout);
        report.RootElement.GetProperty("missing").EnumerateArray()
            .Select(p => $"{p.GetProperty("id").GetString()} {p.GetProperty("version").GetString()}")
            .Should().Equal("FastEndpoints 8.3.0", "protobuf-net 3.2.56");
        report.RootElement.GetProperty("different").EnumerateArray()
            .Select(p => $"{p.GetProperty("id").GetString()} {p.GetProperty("ours").GetString()} {p.GetProperty("theirs").GetString()}")
            .Should().Equal("MediatR.Contracts 2.0.1 1.0.0");
        result.Stderr.Should().Contain("Blocks.Web: copied");
    }

    [Fact]
    public void PackageReport_LeavesOutPinsForBlocksNotTaken()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        var result = _f.Forward("--json");

        using var report = JsonDocument.Parse(result.Stdout);
        report.RootElement.GetProperty("missing").EnumerateArray().Select(p => p.GetProperty("id").GetString())
            .Should().Equal("Humanizer.Core");
        report.RootElement.GetProperty("different").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void PackageReport_AppWithEveryVersion_SaysSo()
    {
        _f.WriteApp("Directory.Packages.props", AppPackages);
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        _f.Forward().Stdout.Should().EndWith("packages: the app's Directory.Packages.props has every version these blocks need\n");
    }

    [Fact]
    public void Forward_NeverEditsTheAppsPackagesFile()
    {
        _f.WriteApp("Directory.Packages.props", AppPackages);
        var before = File.ReadAllBytes(Path.Combine(_f.App, "Directory.Packages.props"));
        _f.WriteManifest(_f.InitialCommit, "Blocks.Web");

        _f.Forward();

        File.ReadAllBytes(Path.Combine(_f.App, "Directory.Packages.props")).Should().Equal(before);
    }

    [Fact]
    public void Forward_AppPackagesFileUnreadable_StopsTheRunBeforeAnyWrite()
    {
        _f.WriteApp("Directory.Packages.props", "<Project><oops></Project>");
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Run("forward");

        result.ExitCode.Should().Be(2, result.Output);
        result.Stderr.Should().Contain("Directory.Packages.props cannot be read");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Forward_NoCentralFileHereAtTheCommit_StopsTheRunBeforeAnyWrite()
    {
        File.Delete(Path.Combine(_f.Source, "Directory.Packages.props"));
        _f.WriteManifest(_f.CommitSource("no central file"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Run("forward");

        result.ExitCode.Should().Be(2, result.Output);
        result.Stderr.Should().Contain("has no Directory.Packages.props at");
        _f.Listing().Should().Equal(before);
    }
}
