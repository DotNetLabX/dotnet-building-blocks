using AwesomeAssertions;
using Xunit;

namespace Blocks.Sync.Tests;

public sealed class StatusTests : IDisposable
{
    private readonly SyncFixture _f = new();

    public void Dispose() => _f.Dispose();

    private void TakeCore()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.Forward();
    }

    private string CommitChangeHere()
    {
        _f.WriteSource("src/Blocks.Core/Strings/Casing.cs", "namespace Blocks.Core.Strings;\n// here edit\n");
        return _f.CommitSource("change here");
    }

    [Fact]
    public void Flow3_InStep()
    {
        TakeCore();

        _f.Status().Stdout.Should().Be("Blocks.Core: in step\n");
    }

    [Fact]
    public void Flow3_ChangedInTheApp()
    {
        TakeCore();
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// app edit\n");

        _f.Status().Stdout.Should().Be("Blocks.Core: changed in the app\n");
    }

    [Fact]
    public void Flow3_ChangedHere()
    {
        TakeCore();
        _f.WriteManifest(CommitChangeHere(), "Blocks.Core");

        _f.Status().Stdout.Should().Be("Blocks.Core: changed here\n");
    }

    [Fact]
    public void Flow3_ChangedOnBothSides()
    {
        TakeCore();
        _f.WriteManifest(CommitChangeHere(), "Blocks.Core");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// app edit\n");

        _f.Status().Stdout.Should().Be("Blocks.Core: changed on both sides\n");
    }

    [Fact]
    public void Flow3_NoLongerListed()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Domain");
        _f.Forward();
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");

        _f.Status().Stdout.Should().Be("Blocks.Core: in step\nBlocks.Domain: no longer listed\n");
    }

    [Fact]
    public void Flow3_NotYetTaken()
    {
        TakeCore();
        _f.WriteManifest(_f.InitialCommit, "Blocks.Domain");

        _f.Status().Stdout.Should().Be("Blocks.Core: in step\nBlocks.Domain: not yet taken\n");
    }

    [Fact]
    public void Flow3_NotYetTaken_WithAnExistingFolder_SaysTheFirstTakeNeedsAdopt()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n");

        _f.Status().Stdout.Should().Be("Blocks.Core: not yet taken; the folder exists, so the first take needs --adopt\n");
    }

    [Fact]
    public void Flow3_NewerHere_WhenTheCheckoutsHeadHoldsAnotherVersion()
    {
        TakeCore();
        CommitChangeHere();

        _f.Status().Stdout.Should().Be("Blocks.Core: in step; newer here\n");
    }

    [Fact]
    public void Flow3_UncommittedHere_WhenTheWorkingTreeDiffersFromHead()
    {
        TakeCore();
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// uncommitted\n");

        _f.Status().Stdout.Should().Be("Blocks.Core: in step; uncommitted here\n");
    }

    [Fact]
    public void Flow3_BothMarks()
    {
        TakeCore();
        CommitChangeHere();
        _f.WriteSource("src/Blocks.Core/Text.cs", "namespace Blocks.Core;\n// uncommitted\n");

        _f.Status().Stdout.Should().Be("Blocks.Core: in step; newer here; uncommitted here\n");
    }

    [Fact]
    public void Flow3_StatusWritesNothing()
    {
        TakeCore();
        _f.WriteManifest(CommitChangeHere(), "Blocks.Domain");
        _f.WriteAppBlock("Blocks.Core", "Text.cs", "namespace Blocks.Core;\n// app edit\n");
        var before = _f.Listing();

        _f.Status();

        _f.Listing().Should().Equal(before);
    }

    [Fact]
    public void Rule11_Status_UnreadableLock_StopsTheRun()
    {
        _f.WriteManifest(_f.InitialCommit, "Blocks.Core");
        _f.WriteApp("blocks.lock.json", "[]");

        _f.Refused("status").Stderr.Should().Contain("blocks.lock.json cannot be read");
    }
}
