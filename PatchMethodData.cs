using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace HarmonyAnalyzers;

using static HarmonyAnalyzers.Constants.PatchTargetMethodType;

internal readonly record struct PatchMethodData(
    INamedTypeSymbol PatchClass,
    IMethodSymbol? PatchMethod = null,
    Constants.HarmonyPatchType? PatchType = null,
    INamedTypeSymbol? TargetType = null,
    string? TargetMethodName = null,
    Constants.PatchTargetMethodType? TargetMethodType = null,
    ImmutableArray<INamedTypeSymbol>? ArgumentTypes = null)
{
    public ImmutableArray<AttributeData> SourceAttributes { get; init; } = [];

    public ImmutableArray<AttributeData> GetConflicts(CancellationToken ct)
    {
        ImmutableArray<AttributeData> conflicts = [];

        var argsByParameter = this.SourceAttributes
            .SelectMany(attr => (attr.AttributeConstructor?.Parameters ?? []).Zip(attr.ConstructorArguments, (p, arg) => (attr, p, arg)))
            .GroupBy(triple => triple.p.Name)
            .SelectMany(g => g.GroupBy(triple => triple.p.Type, SymbolEqualityComparer.Default));

        foreach (var argGroup in argsByParameter)
        {
            if (Util.DistinctTypeConstantsCount(argGroup.Select(triple => triple.arg), ct) > 1)
                conflicts = conflicts.AddRange(argGroup.Select(triple => triple.attr));
        }

        return conflicts.Distinct().ToImmutableArray();
    }

#if DEBUG
    public int Conflicts => this.GetConflicts(default).Length;
#endif

    private IEnumerable<TSymbol> GetAllTargetTypeMembers<TSymbol>() where TSymbol : ISymbol =>
        this.TargetType?.GetMembers().OfType<TSymbol>() ?? [];

    public IEnumerable<TSymbol> GetCandidateTargetMembers<TSymbol>() where TSymbol : ISymbol
    {
        var @this = this;
        return @this.GetAllTargetTypeMembers<TSymbol>().Where(s => s.Name == @this.TargetMethodName);
    }

    private TSymbol? GetTargetMember<TSymbol>() where TSymbol : ISymbol
    {
        var @this = this;
        return @this.GetCandidateTargetMembers<TSymbol>().TryExactlyOne();
    }

    public IEnumerable<IMethodSymbol> GetCandidateMethods()
    {
        var @this = this;

        return ((@this.TargetMethodType ?? Normal), @this.ArgumentTypes) switch
        {
            (Normal, null)          => @this.GetCandidateTargetMembers<IMethodSymbol>(),
            (Normal, _)             => @this.GetCandidateTargetMembers<IMethodSymbol>().FindMethodsWithArgs(@this.ArgumentTypes),
            (Getter, _)             => Util.ReturnSeq(@this.GetTargetMember<IPropertySymbol>()?.GetMethod),
            (Setter, _)             => Util.ReturnSeq(@this.GetTargetMember<IPropertySymbol>()?.SetMethod),
            (Constructor, null)     => @this.TargetType?.Constructors.Where(m => !m.IsStatic) ?? [],
            (Constructor, _)        => @this.TargetType?.Constructors.Where(m => !m.IsStatic).FindMethodsWithArgs(@this.ArgumentTypes) ?? [],
            (StaticConstructor, _)  => @this.TargetType?.StaticConstructors ?? [],
            //(Enumerator, _) => null,
            //(Async, _) => null,
            _ => []
        };
    }

    public IMethodSymbol? TargetMethod => this.GetCandidateMethods().TryExactlyOne();

    public bool IsAmbiguousMatch => this.GetCandidateMethods().Count() > 1;

    internal PatchMethodData AddTargetMethodData(AttributeData patchAttribute)
    {
        var patchData = this;

        if (patchAttribute.AttributeConstructor is not { } constructor)
            return patchData;

        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            switch (constructor.Parameters[i].Name)
            {
                case Constants.Parameter_declaringType:
                    patchData = patchData with
                    { 
                        TargetType = patchAttribute.ConstructorArguments[i].Value as INamedTypeSymbol,
                        SourceAttributes = patchData.SourceAttributes.Add(patchAttribute)
                    };
                    break;
                case Constants.Parameter_methodName:
                    patchData = patchData with
                    {
                        TargetMethodName = patchAttribute.ConstructorArguments[i].Value as string,
                        SourceAttributes = patchData.SourceAttributes.Add(patchAttribute)
                    };
                    break;
                case Constants.Parameter_methodType:
                    var value = (int?)patchAttribute.ConstructorArguments[i].Value;
                    patchData = patchData with
                    { 
                        TargetMethodType = value is not null ? (Constants.PatchTargetMethodType)value.Value : null,
                        SourceAttributes = patchData.SourceAttributes.Add(patchAttribute)
                    };
                    break;
                case Constants.Parameter_argumentTypes:
                    patchData = patchData with
                    {
                        ArgumentTypes = patchAttribute.ConstructorArguments[i].Values
                            .Select(c => c.Value as INamedTypeSymbol)
                            .NotNull()
                            .ToImmutableArray(),
                        SourceAttributes = patchData.SourceAttributes.Add(patchAttribute)
                    };
                    break;
            }
        }

        return patchData;
    }

    internal PatchMethodData AddTargetMethodData(IEnumerable<AttributeData> patchAttributes)
    {
        var patchData = this;

        foreach (var attr in patchAttributes)
        {
            patchData = patchData.AddTargetMethodData(attr);
        }

        return patchData;
    }
}
