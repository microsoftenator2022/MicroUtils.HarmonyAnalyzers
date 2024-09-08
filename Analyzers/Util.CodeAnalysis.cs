using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MicroUtils.HarmonyAnalyzers;
public static partial class Util
{
    public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol root, CancellationToken ct)
    {
        yield return root;
        foreach (var child in root.GetNamespaceMembers())
            foreach (var next in GetAllNamespaces(child, ct))
            {
                if (ct.IsCancellationRequested)
                    yield break;

                yield return next;
            }
    }

    public static IEnumerable<INamedTypeSymbol> AllNestedTypesAndSelf(this INamedTypeSymbol type, CancellationToken ct)
    {
        yield return type;
        foreach (var typeMember in type.GetTypeMembers())
        {
            foreach (var nestedType in typeMember.AllNestedTypesAndSelf(ct))
            {
                if (ct.IsCancellationRequested)
                    yield break;

                yield return nestedType;
            }
        }
    }

    public static bool IsAssignable(this INamedTypeSymbol type) =>
        type is
        {
            IsStatic: false,
            IsImplicitClass: false,
            IsScriptClass: false,
            TypeKind: TypeKind.Class or TypeKind.Struct,
            DeclaredAccessibility: Accessibility.Public,
        };

    public static IEnumerable<INamedTypeSymbol> GetTypes(
        this INamespaceSymbol ns,
        CancellationToken ct,
        bool includeNested = false)
    {
        IEnumerable<INamedTypeSymbol> types = ns.GetTypeMembers();

        if (includeNested)
            types = types.SelectMany(t => t.AllNestedTypesAndSelf(ct));

        return types
            .Where(t => t.CanBeReferencedByName);
    }

    public static IEnumerable<(INamespaceSymbol @namespace, IEnumerable<INamedTypeSymbol> types)> GetTypesByNamespace(
        this Compilation compilation,
        CancellationToken ct,
        bool includeNested = false) =>
        compilation.SourceModule.ReferencedAssemblySymbols
            .Append(compilation.Assembly)
            .SelectMany(a => a.GlobalNamespace.GetAllNamespaces(ct))
            .Select(ns => (ns, ns.GetTypes(ct, includeNested)));

    public static IEnumerable<INamedTypeSymbol> GetTypes(
        this Compilation compilation,
        CancellationToken ct,
        bool includeNested = false) =>
        compilation.GetTypesByNamespace(ct, includeNested)
            .SelectMany(pair => pair.types);

    // probably doesn't work for nested types?
    public static INamedTypeSymbol? GetType(
        this Compilation compilation,
        string @namespace,
        string name,
        CancellationToken ct) =>
        compilation.GetTypesByNamespace(ct)
            .FirstOrDefault(pair => pair.@namespace.Name == @namespace).types
            ?.FirstOrDefault(t => t.Name == name);

    public static IEnumerable<IMethodSymbol> FindMethodsWithArgs(this IEnumerable<IMethodSymbol> source, IEnumerable<ITypeSymbol> argTypes) =>
        source.Where(m =>
            m.Parameters.Length == argTypes.Count() &&
            m.Parameters.Zip(argTypes, (p, arg) => p.Type.Equals(arg, SymbolEqualityComparer.Default)).All(b => b));

    public static IMethodSymbol? FindMethodWithArgs(this IEnumerable<IMethodSymbol> source, IEnumerable<ITypeSymbol> argTypes) =>
        source.FindMethodsWithArgs(argTypes).TryExactlyOne();

    public static int DistinctTypedConstantsCount(IEnumerable<TypedConstant> typedConstants, CancellationToken ct)
    {
        if (!typedConstants.Any())
            return 0;

        return typedConstants.Distinct().Count();
    }

    public static string GetFullMetadataName(this ISymbol s)
    {
        static bool IsRootNamespace(ISymbol symbol) =>
            symbol is INamespaceSymbol ns && ns.IsGlobalNamespace;

        if (s is null || IsRootNamespace(s))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(s.MetadataName);
        var last = s;

        s = s.ContainingSymbol;

        while (!IsRootNamespace(s))
        {
            if (s is ITypeSymbol && last is ITypeSymbol)
            {
                sb.Insert(0, '+');
            }
            else
            {
                sb.Insert(0, '.');
            }

            sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            //sb.Insert(0, s.MetadataName);
            s = s.ContainingSymbol;
        }

        return sb.ToString();
    }

    public static bool IsStandardImplicit(this Conversion conversion) => conversion.IsImplicit && !conversion.IsUserDefined;

    static readonly Regex TypeNameRegex = new(@"^([\w\.`]+)(?:\[.+?\])?(\[\])?$");
    public static string GetMetadataName(this Type type)
    {
        var match = TypeNameRegex.Match(type.ToString());

        return match.Groups[1].Value + match.Groups[2].Value;
    }

    public static INamedTypeSymbol? ToNamedTypeSymbol(this Type type, Compilation compilation) =>
        compilation.GetTypeByMetadataName(type.GetMetadataName());
}

public static class Optional
{
    public static Optional<T> Value<T>(T value) => new(value);
    public static Optional<T> NoValue<T>() => new();
    public static Optional<T> MaybeValue<T>(T? maybeValue) => maybeValue is { } value ? Value(value) : NoValue<T>();

    public static Optional<T> TryFirst<T>(this IEnumerable<T> source)
    {
        foreach (var element in source)
            return element;

        return default;
    }

    public static Optional<T> TryFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var element in source.Where(predicate))
            return element;

        return default;
    }

    public static Optional<T> TrySingle<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Optional<T> value = default;

        foreach (var element in source.Where(predicate))
        {
            if (value.HasValue)
                return default;

            value = element;
        }

        return value;
    }

    public static Optional<T> TrySingle<T>(this IEnumerable<T> source) => source.TrySingle(_ => true);

    public static IEnumerable<U> Choose<T, U>(this IEnumerable<T> source, Func<T, Optional<U>> chooser) =>
        source
            .SelectWhere(chooser, maybeElement => maybeElement.HasValue)
            .Select(element => element.Value);

    public static Optional<U> TryPick<T, U>(this IEnumerable<T> source, Func<T, Optional<U>> picker) =>
        source.Choose(picker).TryFirst();
}
