using System.Text;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class BackTests : IDisposable
{
    private const string AppEdit = "namespace Blocks.Core;\n// app edit\n";

    private readonly SyncFixture _f = new();

    public BackTests()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
    }

    public void Dispose() => _f.Dispose();

    [Fact]
    public void Flow2_Back_CopiesTheAppsBlockWholeIntoTheWorkingTreeWithoutAByteOrderMark()
    {
        SyncFixture.WriteBytes(_f.AppBlockPath("Blocks.Core", "Text.cs"), [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(AppEdit)]);
        _f.WriteAppBlock("Blocks.Core", "Added.cs", "namespace Blocks.Core;\n");
        File.Delete(_f.AppBlockPath("Blocks.Core", "Strings/Casing.cs"));
        var lockBefore = File.ReadAllBytes(_f.LockPath);

        _f.Back();

        File.ReadAllBytes(Path.Combine(_f.Source, "src/Blocks.Core/Text.cs")).Should().Equal(Encoding.UTF8.GetBytes(AppEdit));
        _f.ReadSource("src/Blocks.Core/Added.cs").Should().Be("namespace Blocks.Core;\n");
        File.Exists(Path.Combine(_f.Source, "src/Blocks.Core/Strings/Casing.cs")).Should().BeFalse();
        File.ReadAllBytes(_f.LockPath).Should().Equal(lockBefore);
    }

    [Fact]
    public void Rule4_Back_WritesOnlyThisRepositorysBlockFolder()
    {
        _f.WriteAppBlock("Blocks.Core", "Text.cs", AppEdit);
        string[] allowed = [Path.Combine(_f.Source, "src", "Blocks.Core")];
        var before = _f.Listing(allowed);

        _f.Back();

        _f.Listing(allowed).Should().Equal(before);
    }

    [Fact]
    public void Back_BlockUnchangedInTheApp_LeavesTheWorkingTreeAlone()
    {
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// here, uncommitted\n");

        _f.Back();

        _f.ReadSource("src/Blocks.Core/Strings/Casing.cs").Should().Be("namespace Blocks.Core.Strings;\n// here, uncommitted\n");
    }

    [Fact]
    public void Back_BothSidesEqual_IsInStepAndWritesNothing()
    {
        _f.WriteAppBlock("Blocks.Core", "Text.cs", AppEdit);
        _f.WriteSource("src/Blocks.Core/Text.cs", AppEdit);
        var before = _f.Listing();

        var result = _f.Back();

        result.Stdout.Should().Contain("Blocks.Core: in step");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule2_Back_NeverOverwritesAnUncommittedChangeMadeHere()
    {
        _f.WriteAppBlock("Blocks.Core", "Text.cs", AppEdit);
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// here, uncommitted\n");
        var before = _f.Listing();

        var result = _f.Refused("back");

        result.Stderr.Should().Contain("refused: Blocks.Core changed on both sides\n  in the app:\n    changed Text.cs\n  here:\n    changed Strings/Casing.cs\n")
            .And.Contain(SyncFixture.WayOut);
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_Back_BlockMissingUnderThisRepositorysSrc_IsRefused()
    {
        _f.WriteAppBlock("Blocks.Core", "Text.cs", AppEdit);
        Directory.Delete(Path.Combine(_f.Source, "src", "Blocks.Core"), recursive: true);
        var before = _f.Listing();

        var result = _f.Refused("back");

        result.Stderr.Should().Contain("Blocks.Core does not exist under src/ in");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_Back_JunctionOnTheSendBackPath_IsRefusedWithNothingWritten()
    {
        _f.WriteAppBlock("Blocks.Core", "Text.cs", AppEdit);
        var elsewhere = Path.Combine(_f.Root, "elsewhere");
        Directory.CreateDirectory(elsewhere);
        Directory.Move(Path.Combine(_f.Source, "src"), Path.Combine(elsewhere, "src"));
        SyncFixture.CreateJunction(Path.Combine(_f.Source, "src"), Path.Combine(elsewhere, "src"));
        var before = _f.Listing();

        var result = _f.Refused("back");

        result.Stderr.Should().Contain("Blocks.Core: ").And.Contain("src is a symbolic link or junction");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule10_TheWayOutOfChangedOnBothSides_FollowedToTheEnd()
    {
        _f.WriteAppBlock("Blocks.Core", "Text.cs", AppEdit);
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// here edit\n");
        var second = _f.CommitSource("change here");
        _f.WriteManifest(second, "Blocks.Core");
        _f.Refused("forward").Stderr.Should().Contain("changed on both sides");

        var setAside = _f.ReadAppBlock("Blocks.Core", "Text.cs");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", SyncFixture.Git(_f.Source, "show", $"{_f.InitialCommit}:src/Blocks.Core/Text.cs"));
        _f.Forward();
        _f.ReadAppBlock("Blocks.Core", "Strings/Casing.cs").Should().Be("namespace Blocks.Core.Strings;\n// here edit\n");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", setAside);
        _f.Back();
        var third = _f.CommitSource("the app's edit, sent back");
        _f.WriteManifest(third, "Blocks.Core");

        var result = _f.Forward();

        result.Stdout.Should().Contain("Blocks.Core: in step");
        _f.ReadAppBlock("Blocks.Core", "Text.cs").Should().Be(AppEdit);
        _f.ReadLock()["blocks"]!["Blocks.Core"]!["commit"]!.GetValue<string>().Should().Be(third);
    }
}
