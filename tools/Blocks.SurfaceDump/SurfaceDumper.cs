using System.Reflection;
using System.Runtime.InteropServices;

namespace Blocks.SurfaceDump;

public static class SurfaceDumper
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IReadOnlyList<string> Dump(IEnumerable<string> assemblyPaths, IEnumerable<string> probeDirectories)
    {
        var targets = assemblyPaths.Select(Path.GetFullPath).ToList();
        using var context = new MetadataLoadContext(new PathAssemblyResolver(ResolverPaths(targets, probeDirectories)));

        var lines = new List<string>();
        foreach (var path in targets)
        {
            var assembly = context.LoadFromAssemblyPath(path);
            var assemblyName = assembly.GetName().Name!;
            foreach (var type in LoadTypes(assembly).Where(IsSurfaceType))
                lines.AddRange(DumpType(assemblyName, type));
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    private static IEnumerable<string> ResolverPaths(List<string> targets, IEnumerable<string> probeDirectories)
    {
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
            byName.TryAdd(Path.GetFileName(target), target);

        var folders = targets.Select(t => Path.GetDirectoryName(t)!)
            .Concat(probeDirectories.Select(Path.GetFullPath))
            .Concat(SharedFrameworkFolders());
        foreach (var folder in folders.Where(Directory.Exists))
        foreach (var dll in Directory.EnumerateFiles(folder, "*.dll").Order(StringComparer.Ordinal))
            byName.TryAdd(Path.GetFileName(dll), dll);

        return byName.Values;
    }

    private static IEnumerable<string> SharedFrameworkFolders()
    {
        var runtime = RuntimeEnvironment.GetRuntimeDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        yield return runtime;

        var version = Path.GetFileName(runtime);
        var aspNet = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(runtime)!)!, "Microsoft.AspNetCore.App", version);
        if (Directory.Exists(aspNet))
            yield return aspNet;
    }

    private static Type[] LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var missing = ex.LoaderExceptions.Where(e => e is not null).Select(e => e!.Message).Distinct();
            throw new InvalidOperationException(
                $"Types of {assembly.GetName().Name} could not be loaded; add a --probe folder holding: {string.Join("; ", missing)}");
        }
    }

    private static bool IsSurfaceType(Type type)
    {
        if (!type.IsNested)
            return type.IsPublic;

        return (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem) && IsSurfaceType(type.DeclaringType!);
    }

    private static bool IsVisible(MethodBase? method)
        => method is not null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

    private static bool IsVisible(FieldInfo field)
        => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static IEnumerable<string> DumpType(string assemblyName, Type type)
    {
        var prefix = $"{assemblyName} | {type.Namespace} | {TypeNames.Declared(type)} | ";
        yield return prefix + "type " + Declarations.Type(type);

        if (type.IsEnum)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                yield return prefix + "field " + Declarations.EnumMember(field);
            yield break;
        }

        if (Declarations.IsDelegate(type))
            yield break;

        foreach (var ctor in type.GetConstructors(Declared).Where(c => !c.IsStatic && IsVisible(c)))
            yield return prefix + "ctor " + Declarations.Constructor(type, ctor);

        foreach (var method in type.GetMethods(Declared).Where(IsVisible))
        {
            if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
                continue;
            yield return prefix + "method " + Declarations.Method(method);
        }

        foreach (var property in type.GetProperties(Declared))
        {
            if (IsVisible(property.GetMethod) || IsVisible(property.SetMethod))
                yield return prefix + "property " + Declarations.Property(property);
        }

        foreach (var field in type.GetFields(Declared).Where(IsVisible))
            yield return prefix + "field " + Declarations.Field(field);

        foreach (var @event in type.GetEvents(Declared).Where(e => IsVisible(e.AddMethod)))
            yield return prefix + "event " + Declarations.Event(@event);
    }
}
