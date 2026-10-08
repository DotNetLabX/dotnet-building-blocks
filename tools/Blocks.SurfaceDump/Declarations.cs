using System.Globalization;
using System.Reflection;

namespace Blocks.SurfaceDump;

internal static class Declarations
{
    public static bool IsDelegate(Type type) => type.BaseType?.FullName == "System.MulticastDelegate";

    public static string Type(Type type)
    {
        var access = !type.IsNested || type.IsNestedPublic ? "public" : type.IsNestedFamily ? "protected" : "protected internal";
        var name = TypeNames.Declared(type);
        var constraints = Constraints(type.GetGenericArguments().Where(a => a.DeclaringType == type));

        if (IsDelegate(type))
        {
            var invoke = type.GetMethod("Invoke")!;
            return $"{access} delegate {TypeNames.Render(invoke.ReturnType, NullableFlags.For(invoke.ReturnParameter.CustomAttributes, invoke))} {name}({Parameters(invoke)}){constraints}";
        }

        if (type.IsEnum)
            return $"{access} enum {name} : {TypeNames.Render(type.GetEnumUnderlyingType(), null)}";

        string kind;
        if (type.IsInterface)
            kind = "interface";
        else if (type.IsValueType)
            kind = HasAttribute(type.CustomAttributes, "System.Runtime.CompilerServices.IsReadOnlyAttribute") ? "readonly struct" : "struct";
        else
        {
            var modifiers = type is { IsAbstract: true, IsSealed: true } ? "static "
                : type.IsAbstract ? "abstract "
                : type.IsSealed ? "sealed "
                : "";
            var isRecord = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => m.Name == "<Clone>$");
            kind = modifiers + (isRecord ? "record" : "class");
        }

        var bases = new List<string>();
        if (!type.IsInterface && !type.IsValueType && type.BaseType is { FullName: not "System.Object" } baseType)
            bases.Add(TypeNames.Render(baseType, null));
        bases.AddRange(type.GetInterfaces().Select(i => TypeNames.Render(i, null)).Order(StringComparer.Ordinal));

        var inheritance = bases.Count == 0 ? "" : " : " + string.Join(", ", bases);
        return $"{access} {kind} {name}{inheritance}{constraints}";
    }

    public static string EnumMember(FieldInfo field)
        => $"{field.Name} = {Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)}";

    public static string Constructor(Type type, ConstructorInfo ctor)
        => $"{Access(ctor)} {TypeNames.Short(type)}({Parameters(ctor)})";

    public static string Method(MethodInfo method)
    {
        var generics = method.IsGenericMethodDefinition
            ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">"
            : "";
        var constraints = method.IsGenericMethodDefinition ? Constraints(method.GetGenericArguments()) : "";
        var returns = TypeNames.Render(method.ReturnType, NullableFlags.For(method.ReturnParameter.CustomAttributes, method));
        return $"{Access(method)}{Modifiers(method)} {returns} {method.Name}{generics}({Parameters(method)}){constraints}";
    }

    public static string Property(PropertyInfo property)
    {
        var getter = Visible(property.GetMethod);
        var setter = Visible(property.SetMethod);
        var main = getter ?? setter!;
        var access = Access(main);
        if (getter is not null && setter is not null && Rank(setter) > Rank(getter))
        {
            main = setter;
            access = Access(setter);
        }

        var indexParameters = property.GetIndexParameters();
        var name = indexParameters.Length == 0
            ? property.Name
            : $"this[{string.Join(", ", indexParameters.Select(Parameter))}]";

        var accessors = new List<string>();
        if (getter is not null)
            accessors.Add(Accessor(getter, access, "get"));
        if (setter is not null)
            accessors.Add(Accessor(setter, access, IsInit(setter) ? "init" : "set"));

        var type = TypeNames.Render(property.PropertyType, NullableFlags.For(property.CustomAttributes, property));
        return $"{access}{Modifiers(main)} {type} {name} {{ {string.Join(" ", accessors)} }}";
    }

    public static string Field(FieldInfo field)
    {
        var type = TypeNames.Render(field.FieldType, NullableFlags.For(field.CustomAttributes, field));
        if (field.IsLiteral)
            return $"{Access(field)} const {type} {field.Name} = {Value(field.GetRawConstantValue(), field.FieldType)}";

        var modifiers = (field.IsStatic ? " static" : "") + (field.IsInitOnly ? " readonly" : "");
        return $"{Access(field)}{modifiers} {type} {field.Name}";
    }

    public static string Event(EventInfo @event)
    {
        var type = TypeNames.Render(@event.EventHandlerType!, NullableFlags.For(@event.CustomAttributes, @event));
        return $"{Access(@event.AddMethod!)}{Modifiers(@event.AddMethod!)} {type} {@event.Name}";
    }

    private static MethodInfo? Visible(MethodInfo? method)
        => method is not null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly) ? method : null;

    private static int Rank(MethodBase method) => method.IsPublic ? 2 : method.IsFamilyOrAssembly ? 1 : 0;

    private static string Accessor(MethodInfo accessor, string propertyAccess, string keyword)
    {
        var own = Access(accessor);
        return own == propertyAccess ? keyword + ";" : $"{own} {keyword};";
    }

    private static bool IsInit(MethodInfo setter)
        => setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static string Access(MethodBase method)
        => method.IsPublic ? "public" : method.IsFamily ? "protected" : "protected internal";

    private static string Access(FieldInfo field)
        => field.IsPublic ? "public" : field.IsFamily ? "protected" : "protected internal";

    private static string Modifiers(MethodInfo method)
    {
        var modifiers = new List<string>();
        if (method.IsStatic)
            modifiers.Add("static");

        if (method.IsAbstract)
            modifiers.Add("abstract");
        else if (method.IsVirtual)
        {
            var newSlot = (method.Attributes & MethodAttributes.NewSlot) != 0;
            if (newSlot && !method.IsFinal)
                modifiers.Add("virtual");
            else if (!newSlot)
                modifiers.Add(method.IsFinal ? "sealed override" : "override");
        }

        return modifiers.Count == 0 ? "" : " " + string.Join(" ", modifiers);
    }

    private static string Parameters(MethodBase method)
    {
        var parameters = method.GetParameters();
        var isExtension = method.IsStatic && HasAttribute(method.CustomAttributes, "System.Runtime.CompilerServices.ExtensionAttribute");
        return string.Join(", ", parameters.Select((p, i) => (i == 0 && isExtension ? "this " : "") + Parameter(p)));
    }

    private static string Parameter(ParameterInfo parameter)
    {
        var prefix = "";
        if (HasAttribute(parameter.CustomAttributes, "System.ParamArrayAttribute"))
            prefix = "params ";
        else if (parameter.ParameterType.IsByRef)
            prefix = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";

        var text = $"{prefix}{TypeNames.Render(parameter.ParameterType, NullableFlags.For(parameter.CustomAttributes, parameter.Member))} {parameter.Name}";
        return parameter.HasDefaultValue ? $"{text} = {Value(parameter.RawDefaultValue, parameter.ParameterType)}" : text;
    }

    private static string Value(object? value, Type type) => value switch
    {
        null or DBNull => type.IsValueType && !(type.IsGenericType && type.GetGenericTypeDefinition().FullName == "System.Nullable`1")
            ? "default"
            : "null",
        string s => $"\"{s}\"",
        bool b => b ? "true" : "false",
        char c => $"'{c}'",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static string Constraints(IEnumerable<Type> genericParameters)
    {
        var clauses = new List<string>();
        foreach (var parameter in genericParameters)
        {
            var attributes = parameter.GenericParameterAttributes;
            var isClass = (attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            var isStruct = (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            var types = parameter.GetGenericParameterConstraints()
                .Where(c => !(isStruct && c.FullName == "System.ValueType"))
                .Select(c => TypeNames.Render(c, null))
                .ToList();
            var annotation = GenericAnnotation(parameter);

            var parts = new List<string>();
            if (isClass)
                parts.Add(annotation == 2 ? "class?" : "class");
            else if (isStruct)
                parts.Add(HasAttribute(parameter.CustomAttributes, "System.Runtime.CompilerServices.IsUnmanagedAttribute") ? "unmanaged" : "struct");
            else if (annotation == 1 && types.Count == 0)
                parts.Add("notnull");

            parts.AddRange(types);
            if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !isStruct)
                parts.Add("new()");

            if (parts.Count > 0)
                clauses.Add($" where {parameter.Name} : {string.Join(", ", parts)}");
        }

        return string.Concat(clauses);
    }

    private static byte GenericAnnotation(Type parameter)
        => NullableFlags.FirstByte(parameter.CustomAttributes, NullableFlags.NullableAttribute)
           ?? NullableFlags.ContextOf((MemberInfo?)parameter.DeclaringMethod ?? parameter.DeclaringType);

    private static bool HasAttribute(IEnumerable<CustomAttributeData> attributes, string name)
        => attributes.Any(a => a.AttributeType.FullName == name);
}
