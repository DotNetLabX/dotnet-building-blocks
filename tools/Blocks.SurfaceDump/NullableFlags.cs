using System.Reflection;

namespace Blocks.SurfaceDump;

internal sealed class NullableFlags
{
    public const string NullableAttribute = "System.Runtime.CompilerServices.NullableAttribute";
    public const string NullableContextAttribute = "System.Runtime.CompilerServices.NullableContextAttribute";

    private const byte Annotated = 2;

    private readonly byte[] _flags;
    private readonly byte _all;
    private int _next;

    private NullableFlags(byte[] flags, byte all)
    {
        _flags = flags;
        _all = all;
    }

    public static NullableFlags For(IEnumerable<CustomAttributeData> own, MemberInfo context)
    {
        var attribute = own.FirstOrDefault(a => a.AttributeType.FullName == NullableAttribute);
        if (attribute is null || attribute.ConstructorArguments.Count == 0)
            return new NullableFlags([], ContextOf(context));

        return attribute.ConstructorArguments[0].Value switch
        {
            byte single => new NullableFlags([], single),
            IReadOnlyCollection<CustomAttributeTypedArgument> list => new NullableFlags(list.Select(a => (byte)a.Value!).ToArray(), 0),
            _ => new NullableFlags([], 0),
        };
    }

    public static byte ContextOf(MemberInfo? member)
    {
        for (var current = member; current is not null; current = current.DeclaringType)
        {
            if (FirstByte(current.CustomAttributes, NullableContextAttribute) is { } context)
                return context;
        }

        return 0;
    }

    public static byte? FirstByte(IEnumerable<CustomAttributeData> attributes, string name)
    {
        var attribute = attributes.FirstOrDefault(a => a.AttributeType.FullName == name);
        if (attribute is null || attribute.ConstructorArguments.Count == 0)
            return null;

        return attribute.ConstructorArguments[0].Value switch
        {
            byte b => b,
            IReadOnlyCollection<CustomAttributeTypedArgument> list when list.Count > 0 => (byte)list.First().Value!,
            _ => null,
        };
    }

    public bool NextIsAnnotated()
    {
        if (_flags.Length == 0)
            return _all == Annotated;
        return _next < _flags.Length && _flags[_next++] == Annotated;
    }
}
