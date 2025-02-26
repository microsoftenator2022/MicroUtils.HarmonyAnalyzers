using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;
internal readonly struct PatchClassData
{
    public readonly INamedTypeSymbol ClassSymbol;
    public readonly ImmutableArray<AttributeData> ClassAttributes;
    public readonly ImmutableArray<PatchMethodData> PatchMethods;
    public readonly Compilation Compilation;
    public readonly CommonSymbols CommonSymbols;
    public readonly Lazy<ImmutableArray<IMethodSymbol>> TargetMethodMethods;
    public readonly Lazy<ImmutableArray<IMethodSymbol>> TargetMethodsMethods;

    bool IsTargetMethod(IMethodSymbol m)
    {
        var @this = this;

        return m.Name is HarmonyConstants.TargetMethodMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
            attr.AttributeClass.Equals(
                @this.CommonSymbols.HarmonyTargetMethod,
                SymbolEqualityComparer.Default));
    }

    bool IsTargetMethods(IMethodSymbol m)
    {
        var @this = this;

        return m.Name is HarmonyConstants.TargetMethodsMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
            attr.AttributeClass.Equals(
                @this.CommonSymbols.HarmonyTargetMethods,
                SymbolEqualityComparer.Default));
    }

    ImmutableArray<IMethodSymbol> GetTargetMethodMethods() =>
        this.ClassSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(IsTargetMethod)
            .ToImmutableArray();

    ImmutableArray<IMethodSymbol> GetTargetMethodsMethods() =>
        this.ClassSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(IsTargetMethods)
            .ToImmutableArray();

    public PatchClassData(
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classAttributes,
        ImmutableArray<PatchMethodData> patchMethods,
        Compilation compilation,
        CommonSymbols commonSymbols)
    {
        this.ClassSymbol = classSymbol;
        this.ClassAttributes = classAttributes;
        this.PatchMethods = patchMethods;
        this.Compilation = compilation;
        this.CommonSymbols = commonSymbols;
        this.TargetMethodMethods = new(this.GetTargetMethodMethods);
        this.TargetMethodsMethods = new(this.GetTargetMethodsMethods);
    }
}
