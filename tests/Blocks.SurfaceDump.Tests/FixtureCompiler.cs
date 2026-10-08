using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Blocks.SurfaceDump.Tests;

internal sealed class FixtureCompiler : IDisposable
{
    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();

    private readonly string _folder = Directory.CreateTempSubdirectory("surface-dump-").FullName;
    private int _count;

    public string Compile(string source)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_folder, (++_count).ToString(System.Globalization.CultureInfo.InvariantCulture))).FullName;
        var compilation = CSharpCompilation.Create(
            "Fixture",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var path = Path.Combine(folder, "Fixture.dll");
        var result = compilation.Emit(path);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));

        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
