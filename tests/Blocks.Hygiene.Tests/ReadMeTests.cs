using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed class ReadMeTests
{
    private static readonly string[] BlockHeadings = ["## Purpose", "## Depends on", "## Registration"];

    private const string WayOut =
        "set the app's edit aside, set the app's manifest to the commit here that holds the other change, take the block forward, apply the edit again, then send it back";

    [Fact]
    public void EveryBlock_HasAReadMeWithTheThreeHeadings()
    {
        var blocks = Directory.EnumerateDirectories(Path.Combine(RepoFiles.Root, "src"), "Blocks.*").ToList();
        blocks.Should().HaveCount(12);

        var missing = blocks
            .SelectMany(block => MissingHeadings(Path.Combine(block, "README.md")).Select(heading => $"{Path.GetFileName(block)}: {heading}"))
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, missing).Should().BeEmpty();
    }

    [Fact]
    public void TheDataAccessReadMe_SaysBothSeedingHelpersNeedSqlServer()
    {
        var text = RepoFiles.Text(Path.Combine(RepoFiles.Root, "src", "Blocks.EntityFrameworkCore", "README.md"));

        text.Should().Contain("SQL Server only").And.Contain("ManualGenerateIdScope").And.Contain("TryReseedTable");
    }

    [Fact]
    public void TheRootReadMe_SaysHowToRunTheSyncTool()
    {
        var text = RepoFiles.Text(Path.Combine(RepoFiles.Root, "README.md")).ReplaceLineEndings(" ");
        var words = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        words.Should().Contain("blocks.json").And.Contain("-- forward").And.Contain("-- back").And.Contain("-- status")
            .And.Contain("--adopt").And.Contain(WayOut);
    }

    public static TheoryData<string, string[], string[]> ReadMeStatements => new()
    {
        {
            "src/Blocks.AspNetCore/README.md",
            [
                "An argument error is a fault in the code and answers 500.",
                "A nested field name has every part lower-cased: Address.Street becomes address.street.",
                "UseGlobalExceptionMiddleware",
            ],
            ["400 for validation, argument,", "AddCodeFirstGrpcClient"]
        },
        {
            "src/Blocks.FastEndpoints/README.md",
            [
                "UseEnumsAsText",
                "UseFieldMapValidationReply",
                "A nested field name has every part lower-cased: Address.Street becomes address.street.",
            ],
            ["protobuf"]
        },
        {
            "src/Blocks.Domain/README.md",
            ["The who-type of the two-type forms is a reference type, such as string.", "IAuditableActionBy"],
            []
        },
        {
            "src/Blocks.MediatR/README.md",
            ["The behaviour replaces the caller's id whenever a claim is present, for int and who-typed commands alike."],
            []
        },
        {
            "src/Blocks.EntityFrameworkCore/README.md",
            ["Add your own database provider and design package.", "MigrateAsync", "A duplicate unique value on SQL Server or PostgreSQL answers 409"],
            []
        },
        {
            "src/Blocks.Core/README.md",
            ["Only AddAndValidateOptions checks attributes at start-up.", "ValidateObjectMembers", "ValidateEnumeratedItems"],
            ["Options binding that fails at start-up: `AddAndValidateOptions<T>`, `ConfigureOptionsFromSection<T>`"]
        },
        {
            "src/Blocks.Grpc/README.md",
            ["GetSectionByTypeName", "The service URL and the retry bounds are not checked at start-up."],
            []
        },
        {
            "README.md",
            ["`Blocks.Grpc`"],
            ["gRPC client registration"]
        },
    };

    [Theory]
    [MemberData(nameof(ReadMeStatements))]
    public void EachReadMe_SaysWhatTheBlocksDoNow(string readMe, string[] mustHold, string[] mustNoLongerHold)
    {
        var text = RepoFiles.Text(Path.Combine(RepoFiles.Root, readMe)).ReplaceLineEndings(" ");
        var words = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        words.Should().ContainAll(mustHold);
        foreach (var stale in mustNoLongerHold)
            words.Should().NotContain(stale);
    }

    private static IEnumerable<string> MissingHeadings(string readMe)
    {
        if (!File.Exists(readMe))
            return ["README.md missing"];

        var lines = RepoFiles.Text(readMe).Split('\n').Select(l => l.TrimEnd('\r')).ToHashSet(StringComparer.Ordinal);
        return BlockHeadings.Where(heading => !lines.Contains(heading));
    }
}
