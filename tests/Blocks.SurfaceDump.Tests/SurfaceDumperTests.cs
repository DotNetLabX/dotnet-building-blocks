using AwesomeAssertions;
using Blocks.SurfaceDump;
using Xunit;

namespace Blocks.SurfaceDump.Tests;

public sealed class SurfaceDumperTests : IDisposable
{
    private readonly FixtureCompiler _compiler = new();

    public void Dispose() => _compiler.Dispose();

    private IReadOnlyList<string> Dump(string source)
        => SurfaceDumper.Dump([_compiler.Compile(source)], []);

    [Fact]
    public void SameSource_GivesSameDump()
    {
        const string source = "namespace Lib; public class Widget { public int Count(string name) => 0; }";

        Dump(source).Should().Equal(Dump(source));
    }

    [Fact]
    public void RenamedMethod_ChangesDump()
    {
        var before = Dump("namespace Lib; public class Widget { public int Count(string name) => 0; }");
        var after = Dump("namespace Lib; public class Widget { public int Total(string name) => 0; }");

        before.Should().Contain(l => l.Contains("int Count(string name)"));
        after.Should().Contain(l => l.Contains("int Total(string name)"));
        after.Should().NotContain(l => l.Contains("Count("));
    }

    [Fact]
    public void RemovedMember_ChangesDump()
    {
        var before = Dump("namespace Lib; public class Widget { public int Count => 0; public string Name => \"\"; }");
        var after = Dump("namespace Lib; public class Widget { public int Count => 0; }");

        before.Should().Contain(l => l.Contains("string Name { get; }"));
        after.Should().NotContain(l => l.Contains("Name"));
        after.Should().HaveCount(before.Count - 1);
    }

    [Fact]
    public void AddedMember_ChangesDump()
    {
        var before = Dump("namespace Lib; public class Widget { }");
        var after = Dump("namespace Lib; public class Widget { protected virtual void Reset(int times = 2) { } }");

        after.Should().HaveCount(before.Count + 1);
        after.Should().Contain(l => l.Contains("method protected virtual void Reset(int times = 2)"));
    }

    [Fact]
    public void MovedNamespace_ChangesDump()
    {
        var before = Dump("namespace Lib.Old; public interface IWidget { void Run(); }");
        var after = Dump("namespace Lib.New; public interface IWidget { void Run(); }");

        before.Should().OnlyContain(l => l.Contains("| Lib.Old |"));
        after.Should().OnlyContain(l => l.Contains("| Lib.New |"));
    }

    [Fact]
    public void ReturnMadeNullable_ChangesDump()
    {
        var before = Dump("#nullable enable\nnamespace Lib; public class Widget { public string Name() => \"\"; }");
        var after = Dump("#nullable enable\nnamespace Lib; public class Widget { public string? Name() => null; }");

        before.Should().Contain(l => l.Contains("method public string Name()"));
        after.Should().Contain(l => l.Contains("method public string? Name()"));
    }

    [Fact]
    public void UnconstrainedGenericMadeNullable_ChangesDump()
    {
        const string shape = "#nullable enable\nnamespace Lib; public interface ICache { T Get<T>(T value); bool TryGet<T>(string key, out T value); System.Threading.Tasks.Task<T> GetAsync<T>(System.Func<T> factory); } public class Box<T> { public T Value = default!; }";

        var before = Dump(shape);
        var after = Dump(shape.Replace("T>(T value)", "T>(T? value)").Replace("T Get<", "T? Get<").Replace("out T value", "out T? value")
            .Replace("Task<T>", "Task<T?>").Replace("Func<T>", "Func<T?>").Replace("public T Value", "public T? Value"));

        before.Should().Contain(l => l.EndsWith("method public abstract T Get<T>(T value)"))
            .And.Contain(l => l.EndsWith("method public abstract bool TryGet<T>(string key, out T value)"))
            .And.Contain(l => l.EndsWith("method public abstract System.Threading.Tasks.Task<T> GetAsync<T>(System.Func<T> factory)"))
            .And.Contain(l => l.EndsWith("field public T Value"));
        after.Should().Contain(l => l.EndsWith("method public abstract T? Get<T>(T? value)"))
            .And.Contain(l => l.EndsWith("method public abstract bool TryGet<T>(string key, out T? value)"))
            .And.Contain(l => l.EndsWith("method public abstract System.Threading.Tasks.Task<T?> GetAsync<T>(System.Func<T?> factory)"))
            .And.Contain(l => l.EndsWith("field public T? Value"));
    }

    [Fact]
    public void ClassMadeSealed_ChangesDump()
    {
        var before = Dump("namespace Lib; public class Widget { }");
        var after = Dump("namespace Lib; public sealed class Widget { }");

        before.Should().Contain(l => l.Contains("type public class Widget"));
        after.Should().Contain(l => l.Contains("type public sealed class Widget"));
    }

    [Fact]
    public void GenericConstraints_AreInTheDump()
    {
        var dump = Dump("#nullable enable\nnamespace Lib; public abstract class Store<T> where T : class, new() { public abstract System.Threading.Tasks.Task<T?> FindAsync(int id, System.Threading.CancellationToken ct = default); }");

        dump.Should().Contain(l => l.Contains("type public abstract class Store<T> where T : class, new()"));
        dump.Should().Contain(l => l.Contains("method public abstract System.Threading.Tasks.Task<T?> FindAsync(int id, System.Threading.CancellationToken ct = default)"));
    }

    [Fact]
    public void InternalAndPrivateMembers_AreNotInTheDump()
    {
        var dump = Dump("namespace Lib; public class Widget { internal int A; private int B; private protected int C; } internal class Hidden { }");

        dump.Should().Equal("Fixture | Lib | Widget | ctor public Widget()", "Fixture | Lib | Widget | type public class Widget");
    }
}
