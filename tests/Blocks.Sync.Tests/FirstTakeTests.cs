using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class FirstTakeTests : IDisposable
{
    private readonly SyncFixture _f = new();

    public FirstTakeTests()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// the app's own copy\n");
        _f.WriteAppBlock("Blocks.Core", "AppOnly.cs", "namespace Blocks.Core;\n");
    }

    public void Dispose() => _f.Dispose();

    [Fact]
    public void Rule7_ExistingFolderWithNoLockEntry_IsRefusedWithoutAdopt()
    {
        _f.CommitApp("app copy");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("refused: Blocks.Core already has a folder in the app but no lock entry; run forward with --adopt")
            .And.Contain("  removed AppOnly.cs").And.Contain("  changed Text.cs");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule7_AdoptOnACommittedFolder_ListsTheFilesToRemoveThenReplacesTheFolder()
    {
        _f.CommitApp("app copy");

        var result = _f.Forward("--adopt");

        result.Stdout.Should().Contain("Blocks.Core: first take; removing 1 file(s) from the app's folder:\n  AppOnly.cs\n");
        result.Stdout.IndexOf("AppOnly.cs", StringComparison.Ordinal).Should().BeLessThan(result.Stdout.IndexOf("copied", StringComparison.Ordinal));
        File.Exists(_f.AppBlockPath("Blocks.Core", "AppOnly.cs")).Should().BeFalse();
        _f.ReadAppBlock("Blocks.Core", "Text.cs").Should().Be(_f.ReadSource("src/Blocks.Core/Text.cs"));
        _f.ReadLock()["blocks"]!["Blocks.Core"].Should().NotBeNull();
    }

    [Fact]
    public void Rule7_AdoptWithAnIgnoredFileInTheFolder_IsRefused()
    {
        _f.WriteApp(".gitignore", "*.secret\nbin/\n");
        _f.CommitApp("app copy");
        _f.WriteAppBlock("Blocks.Core", "keys.secret", "secret");
        var before = _f.Listing();

        var result = _f.Refused("forward", "--adopt");

        result.Stderr.Should().Contain("refused: Blocks.Core: the app's folder holds files its repository has not committed")
            .And.Contain("!! blocks/Blocks.Core/keys.secret");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule7_AdoptWithAnUncommittedEdit_IsRefused()
    {
        _f.CommitApp("app copy");
        _f.WriteAppBlock("Blocks.Core", "AppOnly.cs", "namespace Blocks.Core;\n// edited\n");

        var result = _f.Refused("forward", "--adopt");

        result.Stderr.Should().Contain(" M blocks/Blocks.Core/AppOnly.cs");
    }

    [Theory]
    [InlineData("--assume-unchanged")]
    [InlineData("--skip-worktree")]
    public void Rule7_AdoptWithAnEditHiddenFromGitStatus_IsRefused(string hidingFlag)
    {
        _f.CommitApp("app copy");
        SyncFixture.Git(_f.App, "update-index", hidingFlag, "blocks/Blocks.Core/AppOnly.cs");
        _f.WriteAppBlock("Blocks.Core", "AppOnly.cs", "namespace Blocks.Core;\n// edit git status does not show\n");
        var before = _f.Listing();

        var result = _f.Refused("forward", "--adopt");

        result.Stderr.Should().Contain("refused: Blocks.Core: the app's folder holds files its repository has not committed")
            .And.Contain("  differs from the app's last commit: AppOnly.cs");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule7_AdoptWithAnUntrackedFolder_IsRefused()
    {
        var result = _f.Refused("forward", "--adopt");

        result.Stderr.Should().Contain("?? blocks/");
    }

    [Fact]
    public void Rule7_AdoptIgnoresIgnoredBuildOutput()
    {
        _f.WriteApp(".gitignore", "bin/\n");
        _f.CommitApp("app copy");
        _f.WriteAppBlock("Blocks.Core", "bin/Debug/Blocks.Core.dll", "binary");

        _f.Forward("--adopt");

        _f.ReadAppBlock("Blocks.Core", "bin/Debug/Blocks.Core.dll").Should().Be("binary");
    }

    [Fact]
    public void Rule7_AfterTheFirstTake_AnAppEditIsRefusedAgain()
    {
        _f.CommitApp("app copy");
        _f.Forward("--adopt");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// edit after adopt\n");

        _f.Refused("forward", "--adopt").Stderr.Should().Contain("Blocks.Core changed in the app");
    }
}
