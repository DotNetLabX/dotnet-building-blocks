using System.Runtime.CompilerServices;
namespace Blocks.Core;

public static class TypeExtensions
{
    public static bool IsRecord(this Type type)
    {
        return type.IsClass &&
                 type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true).Any();
    }
}
