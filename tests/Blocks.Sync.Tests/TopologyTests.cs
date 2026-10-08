using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class TopologyTests : IDisposable
{
    private const string DataAsAFile = "namespace Blocks.Core;\n// Data as a file\n";
    private const string DataAsAFolder = "namespace Blocks.Core;\n// Data/Item.cs\n";

    private readonly SyncFixture _f = new();

    public void Dispose() => _f.Dispose();

    private string SourceData => Path.Combine(_f.Source, "src", "Blocks.Core", "Data");

    private void TakeWithDataAsAFile()
    {
        _f.WriteSource("src/Blocks.Core/Data", DataAsAFile);
        _f.WriteManifest(_f.CommitSource("data as a file"), "Blocks.Core");
        _f.Forward();
    }

    private void TakeWithDataAsAFolder()
    {
        _f.WriteSource("src/Blocks.Core/Data/Item.cs", DataAsAFolder);
        _f.WriteManifest(_f.CommitSource("data as a folder"), "Blocks.Core");
        _f.Forward();
    }

    [Fact]
    public void Forward_FileThatBecameAFolderHere_IsReplacedInTheApp()
    {
        TakeWithDataAsAFile();
        File.Delete(SourceData);
        _f.WriteSource("src/Blocks.Core/Data/Item.cs", DataAsAFolder);
        _f.WriteManifest(_f.CommitSource("data as a folder"), "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Data/Item.cs").Should().Be(DataAsAFolder);
        _f.Status().Stdout.Should().Be("Blocks.Core: in step\n");
    }

    [Fact]
    public void Forward_FolderThatBecameAFileHere_IsReplacedInTheApp()
    {
        TakeWithDataAsAFolder();
        Directory.Delete(SourceData, recursive: true);
        _f.WriteSource("src/Blocks.Core/Data", DataAsAFile);
        _f.WriteManifest(_f.CommitSource("data as a file"), "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Data").Should().Be(DataAsAFile);
        _f.Status().Stdout.Should().Be("Blocks.Core: in step\n");
    }

    [Fact]
    public void Forward_FolderWithAnEmptyFolderBesideItsFileThatBecameAFileHere_IsReplacedInTheApp()
    {
        TakeWithDataAsAFolder();
        Directory.CreateDirectory(_f.AppBlockPath("Blocks.Core", "Data/Empty"));
        Directory.Delete(SourceData, recursive: true);
        _f.WriteSource("src/Blocks.Core/Data", DataAsAFile);
        _f.WriteManifest(_f.CommitSource("data as a file"), "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Data").Should().Be(DataAsAFile);
        _f.Status().Stdout.Should().Be("Blocks.Core: in step\n");
    }

    [Fact]
    public void Forward_NewFileWhereTheAppHasEmptyFolders_IsWrittenInTheirPlace()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        Directory.CreateDirectory(_f.AppBlockPath("Blocks.Core", "Data/Empty/Deeper"));
        _f.WriteSource("src/Blocks.Core/Data", DataAsAFile);
        _f.WriteManifest(_f.CommitSource("data as a file"), "Blocks.Core");

        _f.Forward();

        _f.ReadAppBlock("Blocks.Core", "Data").Should().Be(DataAsAFile);
        _f.Status().Stdout.Should().Be("Blocks.Core: in step\n");
    }

    [Fact]
    public void Forward_FolderHoldingAJunctionThatMustBecomeAFile_IsRefusedWithNothingWritten()
    {
        TakeWithDataAsAFolder();
        var elsewhere = Path.Combine(_f.Root, "elsewhere");
        SyncFixture.WriteBytes(Path.Combine(elsewhere, "Kept.txt"), "kept"u8.ToArray());
        var link = Path.Combine(_f.AppBlockPath("Blocks.Core", "Data"), "Link");
        SyncFixture.CreateJunction(link, elsewhere);
        Directory.Delete(SourceData, recursive: true);
        _f.WriteSource("src/Blocks.Core/Data", DataAsAFile);
        _f.WriteManifest(_f.CommitSource("data as a file"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain($"refused: {link} is a symbolic link or junction");
        _f.Listing().Should().Equal(before);
        File.Exists(Path.Combine(elsewhere, "Kept.txt")).Should().BeTrue();
    }

    [Fact]
    public void Back_FolderWithAnEmptyFolderBesideItsFileThatBecameAFileInTheApp_IsReplacedHere()
    {
        TakeWithDataAsAFolder();
        Directory.CreateDirectory(Path.Combine(SourceData, "Empty"));
        Directory.Delete(_f.AppBlockPath("Blocks.Core", "Data"), recursive: true);
        _f.WriteAppBlock("Blocks.Core", "Data", DataAsAFile);

        _f.Back();

        _f.ReadSource("src/Blocks.Core/Data").Should().Be(DataAsAFile);
    }

    [Fact]
    public void Back_NewFileWhereThisRepoHasEmptyFolders_IsWrittenInTheirPlace()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
        Directory.CreateDirectory(Path.Combine(SourceData, "Empty", "Deeper"));
        _f.WriteAppBlock("Blocks.Core", "Data", DataAsAFile);

        _f.Back();

        _f.ReadSource("src/Blocks.Core/Data").Should().Be(DataAsAFile);
    }

    [Fact]
    public void Back_FileThatBecameAFolderInTheApp_IsReplacedHere()
    {
        TakeWithDataAsAFile();
        File.Delete(_f.AppBlockPath("Blocks.Core", "Data"));
        _f.WriteAppBlock("Blocks.Core", "Data/Item.cs", DataAsAFolder);

        _f.Back();

        _f.ReadSource("src/Blocks.Core/Data/Item.cs").Should().Be(DataAsAFolder);
    }

    [Fact]
    public void Back_FolderThatBecameAFileInTheApp_IsReplacedHere()
    {
        TakeWithDataAsAFolder();
        Directory.Delete(_f.AppBlockPath("Blocks.Core", "Data"), recursive: true);
        _f.WriteAppBlock("Blocks.Core", "Data", DataAsAFile);

        _f.Back();

        _f.ReadSource("src/Blocks.Core/Data").Should().Be(DataAsAFile);
    }

    [Fact]
    public void Forward_FolderHoldingBuildOutputThatMustBecomeAFile_IsRefusedWithNothingWritten()
    {
        TakeWithDataAsAFolder();
        _f.WriteAppBlock("Blocks.Core", "Data/bin/Item.dll", "build output");
        Directory.Delete(SourceData, recursive: true);
        _f.WriteSource("src/Blocks.Core/Data", DataAsAFile);
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// v2\n");
        _f.WriteManifest(_f.CommitSource("data as a file"), "Blocks.Core");
        var before = _f.Listing();

        var result = _f.Refused("forward");

        result.Stderr.Should().Contain("refused: Blocks.Core: Data cannot replace the folder of that name; it holds files outside the block's files")
            .And.Contain("  Data/bin/Item.dll");
        _f.Listing().Should().Equal(before);
    }
}
