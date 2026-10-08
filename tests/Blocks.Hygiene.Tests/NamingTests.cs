using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Blocks.Hygiene.Tests;

public sealed partial class NamingTests
{
    private static readonly string[] BlockAssemblies =
    [
        "Blocks.AspNetCore", "Blocks.Core", "Blocks.Domain", "Blocks.EntityFrameworkCore", "Blocks.Exceptions",
        "Blocks.FastEndpoints", "Blocks.Hasura", "Blocks.Http.Abstractions", "Blocks.MediatR", "Blocks.Messaging",
        "Blocks.Redis",
    ];

    private static readonly HashSet<string> FrameworkFixedNames = new(StringComparer.Ordinal) { "Handle", "Consume", "Invoke" };

    [GeneratedRegex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();

    [Fact]
    public void EveryFolderInSrc_DeclaresOneNamespace()
    {
        var mixedFolders = RepoFiles.Under("src", "*.cs")
            .Select(file => (Folder: RepoFiles.Relative(Path.GetDirectoryName(file)!), Match: NamespaceDeclaration().Match(RepoFiles.Text(file))))
            .Where(f => f.Match.Success)
            .GroupBy(f => f.Folder, f => f.Match.Groups[1].Value)
            .Where(g => g.Distinct().Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Distinct().Order(StringComparer.Ordinal))}")
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, mixedFolders).Should().BeEmpty();
    }

    [Fact]
    public void EveryTaskReturningMethodInTheBlocks_EndsInAsync()
    {
        var misnamed = BlockAssemblies
            .Select(name => Assembly.Load(new AssemblyName(name)))
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(ReturnsTask)
            .Where(method => !method.Name.EndsWith("Async", StringComparison.Ordinal))
            .Where(method => !IsExempt(method))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .Distinct()
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, misnamed).Should().BeEmpty();
    }

    [Fact]
    public void EveryClassOfExtensionMethodsInTheBlocks_EndsInExtensions()
    {
        var misnamed = BlockAssemblies
            .Select(name => Assembly.Load(new AssemblyName(name)))
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: true, IsSealed: true, IsNested: false })
            .Where(IsAClassOfExtensionMethods)
            .Where(type => !type.Name.EndsWith("Extensions", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal);

        string.Join(Environment.NewLine, misnamed).Should().BeEmpty();
    }

    private static bool IsAClassOfExtensionMethods(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        return methods.Length > 0 && methods.All(method => method.IsDefined(typeof(ExtensionAttribute), false));
    }

    private static bool ReturnsTask(MethodInfo method)
    {
        var type = method.ReturnType.IsGenericType ? method.ReturnType.GetGenericTypeDefinition() : method.ReturnType;
        return type == typeof(Task) || type == typeof(Task<>) || type == typeof(ValueTask) || type == typeof(ValueTask<>);
    }

    private static bool IsExempt(MethodInfo method)
        => FrameworkFixedNames.Contains(method.Name)
           || method.IsSpecialName
           || method.Name.Contains('<')
           || method.IsDefined(typeof(CompilerGeneratedAttribute), false)
           || method.GetBaseDefinition() != method;
}
