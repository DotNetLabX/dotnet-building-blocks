using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class ContainmentTests : IDisposable
{
    private readonly SyncFixture _f = new();

    public void Dispose() => _f.Dispose();

    [Theory]
    [InlineData("../Blocks.Core")]
    [InlineData("Blocks.Core/..")]
    [InlineData("Blocks..Core")]
    [InlineData("Other.Core")]
    public void Rule4_BlockNameThatIsNotABlockName_IsRefusedWithNothingWritten(string block)
    {
        _f.WriteManifest(_f.InitialCommit, block);
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain($"block name '{block}' in blocks.json is not a block name");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_BlockThatDoesNotExistUnderSrc_IsRefused()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Missing");

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("Blocks.Missing does not exist under src/");
        File.Exists(_f.LockPath).Should().BeFalse();
    }

    [Fact]
    public void Rule4_RootedBlocksFolder_IsRefusedWithNothingWritten()
    {
        var outside = Path.Combine(_f.Root, "outside");
        _f.WriteManifestIn(outside, _f.InitialCommit, "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("blocksFolder").And.Contain("must be a relative path");
        Directory.Exists(outside).Should().BeFalse();
        _f.Listing().Should().Equal(before);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("blocks/../../outside")]
    public void Rule4_BlocksFolderWithParentSegment_IsRefused(string blocksFolder)
    {
        _f.WriteManifestIn(blocksFolder, _f.InitialCommit, "Blocks.Core");
        var before = _f.Listing();

        _f.Refused("forward").Stderr.Should().Contain("blocksFolder");

        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_LockPathEscapingTheApp_IsRefusedWithNothingWritten()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var lockFile = _f.ReadLock();
        lockFile["blocks"]!["Blocks.Core"]!["files"]!.AsObject()["../../../outside.txt"] = new string('0', 64);
        _f.WriteApp("blocks.lock.json", lockFile.ToJsonString());
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("blocks.lock.json path of Blocks.Core '../../../outside.txt' must be a relative path");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_JunctionOnAWritePath_IsRefusedWithNothingWritten()
    {
        var outside = Path.Combine(_f.Root, "outside");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(Path.Combine(_f.App, SyncFixture.BlocksFolder));
        SyncFixture.CreateJunction(_f.AppBlockPath("Blocks.Core"), outside);
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("is a symbolic link or junction");
        Directory.EnumerateFileSystemEntries(outside).Should().BeEmpty();
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_JunctionAboveTheBlockFolder_IsRefusedWithNothingWritten()
    {
        var outside = Path.Combine(_f.Root, "outside");
        Directory.CreateDirectory(outside);
        SyncFixture.CreateJunction(Path.Combine(_f.App, SyncFixture.BlocksFolder), outside);
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("Blocks.Core: ").And.Contain("is a symbolic link or junction");
        Directory.EnumerateFileSystemEntries(outside).Should().BeEmpty();
    }

    [Fact]
    public void Rule4_JunctionInsideTheBlockFolder_IsRefusedWithNothingWritten()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var outside = Path.Combine(_f.Root, "outside");
        Directory.CreateDirectory(outside);
        File.Copy(_f.AppBlockPath("Blocks.Core", "Strings/Casing.cs"), Path.Combine(outside, "Casing.cs"));
        Directory.Delete(_f.AppBlockPath("Blocks.Core", "Strings"), recursive: true);
        SyncFixture.CreateJunction(_f.AppBlockPath("Blocks.Core", "Strings"), outside);
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("Strings is a symbolic link or junction");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_HardLinkedDestinationFile_LeavesTheOutsideFileUnchanged()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var outside = Path.Combine(_f.Root, "outside", "Text.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(outside)!);
        var appText = _f.AppBlockPath("Blocks.Core", "Text.cs");
        File.Copy(appText, outside);
        File.Delete(appText);
        SyncFixture.CreateHardLink(appText, outside);
        var outsideContent = File.ReadAllText(outside);
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");
        string[] allowed = [_f.AppBlockPath("Blocks.Core"), _f.LockPath];
        var before = _f.Listing(allowed);

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Text.cs").Should().Be("namespace Blocks.Core;\n// v2\n");
        File.ReadAllText(outside).Should().Be(outsideContent);
        _f.Listing(allowed).Should().Equal(before);
    }

    [Fact]
    public void Rule4_Forward_WritesOnlyTheBlockFoldersAndTheLock()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Domain");
        string[] allowed = [_f.AppBlockPath("Blocks.Domain"), _f.AppBlockPath("Blocks.Core"), _f.LockPath];
        var before = _f.Listing(allowed);

        _f.Forward();

        _f.Listing(allowed).Should().Equal(before);
        Directory.EnumerateFileSystemEntries(Path.Combine(_f.App, SyncFixture.BlocksFolder)).Select(Path.GetFileName)
            .Should().BeEquivalentTo("Blocks.Core", "Blocks.Domain");
    }

    [Fact]
    public void Rule11_UnreadableLock_StopsTheRunBeforeAnyWrite()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteApp("blocks.lock.json", "{ not json");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("blocks.lock.json cannot be read");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule11_LockEntryWithNoFolder_StopsTheRunBeforeAnyWrite()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Domain");
        _f.Forward();
        Directory.Delete(_f.AppBlockPath("Blocks.Core"), recursive: true);
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("has an entry for Blocks.Core but the app has no folder");
        _f.Listing().Should().Equal(before);
    }

    [Theory]
    [InlineData("bin/Debug/Blocks.Core.dll", "0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("Text.cs", "not-a-fingerprint")]
    [InlineData("Strings\\Casing.cs", "0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("./Text.cs", "0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("Strings//Casing.cs", "0000000000000000000000000000000000000000000000000000000000000000")]
    public void Rule11_LockFileListThatIsNotTheBlocksFiles_StopsTheRunBeforeAnyWrite(string path, string fingerprint)
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var lockFile = _f.ReadLock();
        lockFile["blocks"]!["Blocks.Core"]!["files"]!.AsObject()[path] = JsonValue.Create(fingerprint);
        _f.WriteApp("blocks.lock.json", lockFile.ToJsonString());
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("blocks.lock.json cannot be read").And.Contain("Blocks.Core");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule11_LockEntryWithNoCommit_StopsTheRun()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteApp("blocks/Blocks.Core/Text.cs", "x");
        _f.WriteApp("blocks.lock.json", """{ "blocks": { "Blocks.Core": { "files": {} } } }""");

        _f.Refused("forward").Stderr.Should().Contain("Blocks.Core needs a 'commit' text and a 'files' object");
    }

    [Fact]
    public void Rule11_LockFingerprintThatHidesAnAppEdit_StopsTheRunBeforeAnyWrite()
    {
        const string edit = "namespace Blocks.Core;\n// unsent app edit\n";
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", "Text.cs", edit);
        var lockFile = _f.ReadLock();
        lockFile["blocks"]!["Blocks.Core"]!["files"]!["Text.cs"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(edit)));
        _f.WriteApp("blocks.lock.json", lockFile.ToJsonString());
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("second"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain($"refused: blocks.lock.json cannot be read: Blocks.Core's files are not the block's files at {_f.InitialCommit}")
            .And.Contain("  changed Text.cs");
        _f.ReadAppBlock("Blocks.Core", "Text.cs").Should().Be(edit);
        _f.Listing().Should().Equal(before);
    }

    [Theory]
    [InlineData("forward")]
    [InlineData("back")]
    [InlineData("status")]
    public void Rule11_LockFileListMissingAFileOfTheBlock_StopsTheRunBeforeAnyWrite(string command)
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var lockFile = _f.ReadLock();
        lockFile["blocks"]!["Blocks.Core"]!["files"]!.AsObject().Remove("Strings/Casing.cs");
        _f.WriteApp("blocks.lock.json", lockFile.ToJsonString());
        var before = _f.Listing();

        var result = _f.Refused(command);

        result.Stderr.Should().Contain("blocks.lock.json cannot be read: Blocks.Core's files are not the block's files")
            .And.Contain("  removed Strings/Casing.cs");
        _f.Listing().Should().Equal(before);
    }

    [Theory]
    [InlineData("./blocks")]
    [InlineData("blocks/")]
    [InlineData("./blocks//")]
    public void Rule4_BlocksFolderWithACurrentFolderSegmentOrATrailingSlash_IsTheSameFolder(string blocksFolder)
    {
        _f.WriteManifestIn(blocksFolder, _f.InitialCommit, "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Text.cs").Should().Be(_f.ReadSource("src/Blocks.Core/Text.cs"));
        _f.Status().Stdout.Should().Be("Blocks.Core: in step\n");
    }

    [Theory]
    [InlineData(".")]
    [InlineData("./")]
    public void Rule4_BlocksFolderThatNamesNoFolder_IsRefusedNamingTheCause(string blocksFolder)
    {
        _f.WriteManifestIn(blocksFolder, _f.InitialCommit, "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain($"refused: blocksFolder '{blocksFolder}' names no folder inside the app");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_AppRootThatIsAJunction_IsRefusedWithNothingWritten()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        var link = Path.Combine(_f.Root, "app-link");
        SyncFixture.CreateJunction(link, _f.App);
        var before = _f.Listing();

        var result = SyncFixture.RunIn(link, _f.Source, "forward");

        result.ExitCode.Should().Be(1, result.Output);
        result.Stderr.Should().Contain($"{link} is a symbolic link or junction");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_AppRootThatIsAJunction_IsRefusedEvenWhenOnlyTheLockWouldBeWritten()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        var link = Path.Combine(_f.Root, "app-link");
        SyncFixture.CreateJunction(link, _f.App);
        var before = _f.Listing();

        var result = SyncFixture.RunIn(link, _f.Source, "forward");

        result.ExitCode.Should().Be(1, result.Output);
        result.Stderr.Should().Contain($"blocks.lock.json: {link} is a symbolic link or junction");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule4_Back_SourceRootThatIsAJunction_IsRefusedWithNothingWritten()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// app edit\n");
        var link = Path.Combine(_f.Root, "source-link");
        SyncFixture.CreateJunction(link, _f.Source);
        var before = _f.Listing();

        var result = SyncFixture.RunIn(_f.App, link, "back");

        result.ExitCode.Should().Be(1, result.Output);
        result.Stderr.Should().Contain($"{link} is a symbolic link or junction");
        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void LockCommitMissingInTheCheckout_SaysFetchFirst()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        _f.WriteApp("blocks.lock.json", File.ReadAllText(_f.LockPath).Replace(_f.InitialCommit, new string('b', 40)));

        var result = _f.Run("forward");

        result.ExitCode.Should().Be(2);
        result.Stderr.Should().Contain("fetch first");
    }
}
