using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace HarmonyAnalyzers;
internal static partial class Util
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

    internal static IEnumerable<INamedTypeSymbol> AllNestedTypesAndSelf(this INamedTypeSymbol type, CancellationToken ct)
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

    internal static bool IsAssignable(this INamedTypeSymbol type) =>
        type is
        {
            IsStatic: false,
            IsImplicitClass: false,
            IsScriptClass: false,
            TypeKind: TypeKind.Class or TypeKind.Struct,
            DeclaredAccessibility: Accessibility.Public,
        };

    internal static IEnumerable<INamedTypeSymbol> GetTypes(
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

    internal static IEnumerable<(INamespaceSymbol @namespace, IEnumerable<INamedTypeSymbol> types)> GetTypesByNamespace(
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

    public static int DistinctTypeConstantsCount(IEnumerable<TypedConstant> typedConstants, CancellationToken ct)
    {
        if (!typedConstants.Any())
            return 0;

        if (typedConstants.Select(c => c.Kind).Distinct().Count() > 1)
        {
            return typedConstants
                .GroupBy(c => c.Kind)
                .Select(cs => Math.Max(1, DistinctTypeConstantsCount(cs, ct)))
                .Sum();
        }

        switch (typedConstants.First().Kind)
        {
            case TypedConstantKind.Array:
                var lengthGroups = typedConstants
                    .GroupBy(c => c.IsNull ? -1 : c.Values.Length);

                var count = 0;

                foreach (var lg in lengthGroups)
                {
                    if (ct.IsCancellationRequested)
                        return 0;

                    if (lg.Key < 1)
                    {
                        count++;
                        continue;
                    }

                    for (var i = 0; i < lg.Key; i++)
                    {
                        if (ct.IsCancellationRequested)
                            return 0;
                        
                        count += Math.Max(1, DistinctTypeConstantsCount(lg.Select(c => c.Values[i]), ct));
                    }
                }

                return count;

            case TypedConstantKind.Type:
                return typedConstants
                    .Select(c => c.Value)
                    .OfType<ITypeSymbol>()
                    .Distinct(SymbolEqualityComparer.Default)
                    .Count();

            default:
                return typedConstants
                    .Select(c => c.Value)
                    .Distinct()
                    .Count();
        }
    }

}
