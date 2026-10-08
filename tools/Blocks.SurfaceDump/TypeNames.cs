namespace Blocks.SurfaceDump;

internal static class TypeNames
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["System.Boolean"] = "bool",
        ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte",
        ["System.Char"] = "char",
        ["System.Decimal"] = "decimal",
        ["System.Double"] = "double",
        ["System.Single"] = "float",
        ["System.Int16"] = "short",
        ["System.UInt16"] = "ushort",
        ["System.Int32"] = "int",
        ["System.UInt32"] = "uint",
        ["System.Int64"] = "long",
        ["System.UInt64"] = "ulong",
        ["System.IntPtr"] = "nint",
        ["System.UIntPtr"] = "nuint",
        ["System.Object"] = "object",
        ["System.String"] = "string",
        ["System.Void"] = "void",
    };

    public static string Declared(Type type)
    {
        var chain = NestingChain(type);
        var all = type.GetGenericArguments();
        var parts = new List<string>();
        var used = 0;
        foreach (var segment in chain)
        {
            var count = segment.GetGenericArguments().Length - used;
            parts.Add(Segment(segment.Name, all.Skip(used).Take(count).Select(a => a.Name)));
            used += count;
        }

        return string.Join('.', parts);
    }

    public static string Short(Type type) => StripArity(type.Name);

    public static string Render(Type type, NullableFlags? flags)
    {
        if (type.IsByRef)
            return Render(type.GetElementType()!, flags);

        if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == "System.Nullable`1")
            return Render(type.GetGenericArguments()[0], flags) + "?";

        var hasFlag = !(type.IsValueType && !type.IsGenericType && !type.IsGenericParameter);
        var mark = flags is not null && hasFlag && flags.NextIsAnnotated() && !type.IsValueType ? "?" : "";

        if (type.IsGenericParameter)
            return type.Name + mark;

        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            return Render(type.GetElementType()!, flags) + "[" + new string(',', rank - 1) + "]" + mark;
        }

        if (type.IsPointer)
            return Render(type.GetElementType()!, flags) + "*";

        if (type.FullName is { } fullName && Aliases.TryGetValue(fullName, out var alias))
            return alias + mark;

        var rendered = type.GetGenericArguments().Select(a => Render(a, flags)).ToArray();

        var parts = new List<string>();
        var used = 0;
        foreach (var segment in NestingChain(type))
        {
            var count = segment.GetGenericArguments().Length - used;
            parts.Add(Segment(segment.Name, rendered.Skip(used).Take(count)));
            used += count;
        }

        var ns = type.Namespace is { Length: > 0 } n ? n + "." : "";
        return ns + string.Join('.', parts) + mark;
    }

    private static List<Type> NestingChain(Type type)
    {
        var chain = new List<Type>();
        for (var current = type; current is not null; current = current.IsNested ? current.DeclaringType : null)
            chain.Insert(0, current);
        return chain;
    }

    private static string Segment(string name, IEnumerable<string> arguments)
    {
        var list = arguments.ToList();
        var bare = StripArity(name);
        return list.Count == 0 ? bare : $"{bare}<{string.Join(", ", list)}>";
    }

    private static string StripArity(string name)
    {
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? name : name[..tick];
    }
}
