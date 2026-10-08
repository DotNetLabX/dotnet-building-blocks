using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class ForwardTests : IDisposable
{
    private readonly SyncFixture _f = new();

    public void Dispose() => _f.Dispose();

    [Fact]
    public void Flow1_Forward_CopiesListedBlockWithItsDependenciesAndWritesTheLock()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Domain");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Domain", "Entity.cs").Should().Be(_f.ReadSource("src/Blocks.Domain/Entity.cs"));
        _f.ReadAppBlock("Blocks.Core", "Strings/Casing.cs").Should().Be(_f.ReadSource("src/Blocks.Core/Strings/Casing.cs"));
        Directory.Exists(_f.AppBlockPath("Blocks.Web")).Should().BeFalse();
        var blocks = _f.ReadLock()["blocks"]!;
        blocks["Blocks.Domain"]!["commit"]!.GetValue<string>().Should().Be(_f.InitialCommit);
        blocks["Blocks.Core"]!["files"]!["Strings/Casing.cs"]!.GetValue<string>().Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Rule1_Forward_AddsReplacesAndRemovesFilesSoTheCopyEqualsTheBlock()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        File.Delete(Path.Combine(_f.Source, "src/Blocks.Core/Text.cs"));
        _f.WriteSource("src/Blocks.Core/Added.cs", "namespace Blocks.Core;\n");
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// v2\n");
        var second = _f.CommitSource("second");
        _f.WriteManifest(second, "Blocks.Core");

        _f.Forward();

        File.Exists(_f.AppBlockPath("Blocks.Core", "Text.cs")).Should().BeFalse();
        _f.ReadAppBlock("Blocks.Core", "Added.cs").Should().Be("namespace Blocks.Core;\n");
        _f.ReadAppBlock("Blocks.Core", "Strings/Casing.cs").Should().Be("namespace Blocks.Core.Strings;\n// v2\n");
        _f.ReadLock()["blocks"]!["Blocks.Core"]!["commit"]!.GetValue<string>().Should().Be(second);
    }

    [Fact]
    public void Rule2_Forward_RefusesAnUnsentAppEditAndWritesNothing()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Domain");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// app edit\n");
        _f.WriteSource("src/Blocks.Domain/Entity.cs", "namespace Blocks.Domain;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Domain");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("refused: Blocks.Core changed in the app; send it back first").And.Contain("  changed Text.cs");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Forward_FileAddedInTheApp_IsAChangeInTheApp()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", "Extra.cs", "namespace Blocks.Core;\n");

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("Blocks.Core changed in the app").And.Contain("  added Extra.cs");
    }

    [Fact]
    public void Rule3_Forward_FileRenamedInTheApp_IsAChangeInTheApp()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        File.Move(_f.AppBlockPath("Blocks.Core", "Text.cs"), _f.AppBlockPath("Blocks.Core", "Words.cs"));

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("  added Words.cs").And.Contain("  removed Text.cs");
    }

    [Fact]
    public void Rule3_Forward_LineEndingsAndAByteOrderMarkInTheApp_AreNotAChange()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var text = _f.AppBlockPath("Blocks.Core", "Text.cs");
        SyncFixture.WriteBytes(text, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(File.ReadAllText(text).Replace("\n", "\r\n"))]);
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Strings/Casing.cs").Should().Be("namespace Blocks.Core.Strings;\n// v2\n");
    }

    [Fact]
    public void Forward_WritesThisRepositorysBytesAsStored()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var text = _f.AppBlockPath("Blocks.Core", "Text.cs");
        SyncFixture.WriteBytes(text, Encoding.UTF8.GetBytes(File.ReadAllText(text).Replace("\n", "\r\n")));
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");

        _f.Forward();

        File.ReadAllBytes(text).Should().Equal(Encoding.UTF8.GetBytes("namespace Blocks.Core;\n// v2\n"));
    }

    [Theory]
    [InlineData("bin/Debug/Blocks.Core.dll")]
    [InlineData("obj/project.assets.json")]
    [InlineData(".vs/state.json")]
    [InlineData(".vscode/settings.json")]
    [InlineData(".idea/workspace.xml")]
    [InlineData("Strings/bin/Casing.dll")]
    [InlineData("Blocks.Core.csproj.user")]
    [InlineData("Blocks.Core.suo")]
    [InlineData(".DS_Store")]
    public void Forward_BuildOutputAndEditorFiles_AreIgnoredAndLeftInPlace(string file)
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", file, "local");
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", file).Should().Be("local");
        _f.ReadAppBlock("Blocks.Core", "Strings/Casing.cs").Should().Be("namespace Blocks.Core.Strings;\n// v2\n");
        _f.ReadLock()["blocks"]!["Blocks.Core"]!["files"]!.AsObject().Select(p => p.Key)
            .Should().Equal("Blocks.Core.csproj", "Strings/Casing.cs", "Text.cs");
    }

    [Fact]
    public void Rule5_Forward_NeverCopiesTheTestsOfABlock()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        _f.Forward();

        Directory.EnumerateFiles(_f.App, "TextTests.cs", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public void ThreeWay_BothSidesEqual_IsInStepAndRefreshesTheLock()
    {
        const string same = "namespace Blocks.Core;\n// same\n";
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", "Text.cs", same);
        _f.WriteSource("src/Blocks.Core/Text.cs", same);
        var second = _f.CommitSource("second");
        _f.WriteManifest(second, "Blocks.Core");

        _f.Forward();

        var entry = _f.ReadLock()["blocks"]!["Blocks.Core"]!;
        entry["commit"]!.GetValue<string>().Should().Be(second);
        entry["files"]!["Text.cs"]!.GetValue<string>().Should().Be(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(same))));
    }

    [Fact]
    public void Rule8_DroppedBlock_KeepsItsFolderAndLockEntry()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Web");
        _f.Forward();
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        _f.Forward();

        File.Exists(_f.AppBlockPath("Blocks.Web", "Endpoint.cs")).Should().BeTrue();
        _f.ReadLock()["blocks"]!.AsObject().Select(p => p.Key).Should().Equal("Blocks.Core", "Blocks.Domain", "Blocks.Web");
    }

    [Fact]
    public void Rules6And8_DroppedBlockEditedInTheApp_StillStopsAForwardRun()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Web");
        _f.Forward();
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteAppBlock("Blocks.Web", "Endpoint.cs", "namespace Blocks.Web;\n// app edit\n");

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("refused: Blocks.Web changed in the app").And.Contain("  changed Endpoint.cs");
    }

    [Fact]
    public void Rules9And10_ChangedOnBothSides_IsRefusedWithBothFileListsAndTheWayOut()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// app edit\n");
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// here edit\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("refused: Blocks.Core changed on both sides\n  in the app:\n    changed Text.cs\n  here:\n    changed Strings/Casing.cs\n")
            .And.Contain(SyncFixture.WayOut);
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Forward_ManifestSourceNotThisCheckoutsOrigin_IsRefused()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteApp("blocks.json", File.ReadAllText(Path.Combine(_f.App, "blocks.json")).Replace(SyncFixture.Origin, "https://example.test/other.git"));

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("is not the manifest's source 'https://example.test/other.git'");
        File.Exists(_f.LockPath).Should().BeFalse();
    }

    [Fact]
    public void Forward_ManifestCommitMissingInTheCheckout_SaysFetchFirst()
    {
        _f.WriteManifest(new string('a', 40), "Blocks.Core");

        var result = _f.Run("forward");

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().Contain($"commit {new string('a', 40)} is not in").And.Contain("fetch first");
        File.Exists(_f.LockPath).Should().BeFalse();
    }
}
