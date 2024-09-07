using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers;

using static MicroUtils.HarmonyAnalyzers.HarmonyConstants.PatchTargetMethodType;

public readonly record struct PatchMethodData(
    INamedTypeSymbol PatchClass,
    IMethodSymbol PatchMethod,
    HarmonyConstants.HarmonyPatchType? PatchType = null,
    INamedTypeSymbol? TargetType = null,
    string? TargetMethodName = null,
    HarmonyConstants.PatchTargetMethodType? TargetMethodType = null,
    ImmutableArray<INamedTypeSymbol>? ArgumentTypes = null)
{
    public ImmutableArray<AttributeData> HarmonyPatchAttributes { get; init; } = [];

    //public MethodDeclarationSyntax? SyntaxNode => this.PatchMethod.DeclaringSyntaxReferences
    //    .Select(sr => sr.GetSyntax())
    //    .OfType<MethodDeclarationSyntax>()
    //    .FirstOrDefault();

    public ImmutableArray<AttributeData> GetConflicts(CancellationToken ct)
    {
        ImmutableArray<AttributeData> conflicts = [];

        var argsByParameter = this.HarmonyPatchAttributes
            .SelectMany(attr => (attr.AttributeConstructor?.Parameters ?? []).Zip(attr.ConstructorArguments, (p, arg) => (attr, p, arg)))
            .GroupBy(triple => triple.p.Name)
            .SelectMany(g => g.GroupBy(triple => triple.p.Type, SymbolEqualityComparer.Default));

        foreach (var argGroup in argsByParameter)
        {
            if (Util.DistinctTypedConstantsCount(argGroup.Select(triple => triple.arg), ct) > 1)
                conflicts = conflicts.AddRange(argGroup.Select(triple => triple.attr));
        }

        return conflicts.Distinct().ToImmutableArray();
    }

#if DEBUG
    public int Conflicts => this.GetConflicts(default).Length;
    public string? TargetMetadataName => this.TargetMethod?.GetFullMetadataName();
#endif

    //public bool IsPassthroughPostfix =>
    //    this.PatchType is HarmonyConstants.HarmonyPatchType.Postfix && this.PatchMethod.ReturnTypeMatchesFirstParameter();
       
    public IEnumerable<TSymbol> GetAllTargetTypeMembers<TSymbol>() where TSymbol : ISymbol =>
        this.TargetType?.GetMembers().OfType<TSymbol>() ?? [];

    public IEnumerable<TSymbol> GetCandidateTargetMembers<TSymbol>() where TSymbol : ISymbol
    {
        var @this = this;
        return @this.GetAllTargetTypeMembers<TSymbol>().Where(s => s.Name == @this.TargetMethodName);
    }

    //private TSymbol? GetTargetMember<TSymbol>() where TSymbol : ISymbol
    //{
    //    var @this = this;
    //    return @this.GetCandidateTargetMembers<TSymbol>().TryExactlyOne();
    //}

    private IEnumerable<IPropertySymbol> GetTargetProperties()
    {
        var @this = this;
        return @this.GetAllTargetTypeMembers<IPropertySymbol>()
            .Where(s => string.IsNullOrEmpty(@this.TargetMethodName) ? s.IsIndexer : s.Name == @this.TargetMethodName);
    }

    public IEnumerable<IMethodSymbol> GetCandidateMethods(
        HarmonyConstants.PatchTargetMethodType targetMethodType,
        IEnumerable<INamedTypeSymbol>? argumentTypes)
    {
        var @this = this;

        return (targetMethodType, argumentTypes) switch
        {
            (Normal, null) => @this.GetCandidateTargetMembers<IMethodSymbol>(),
            (Normal, _) => @this.GetCandidateTargetMembers<IMethodSymbol>().FindMethodsWithArgs(argumentTypes),
            (Getter, null) => @this.GetTargetProperties().Select(p => p.GetMethod).NotNull(),
            (Getter, _) => @this.GetTargetProperties().Select(p => p.GetMethod).NotNull().FindMethodsWithArgs(argumentTypes),
            (Setter, null) => @this.GetTargetProperties().Select(p => p.GetMethod).NotNull(),
            (Setter, _) => @this.GetTargetProperties().Select(p => p.GetMethod).NotNull().FindMethodsWithArgs(argumentTypes),
            (Constructor, null) => @this.TargetType?.Constructors.Where(m => !m.IsStatic) ?? [],
            (Constructor, _) => @this.TargetType?.Constructors.Where(m => !m.IsStatic).FindMethodsWithArgs(argumentTypes) ?? [],
            (StaticConstructor, _) => @this.TargetType?.StaticConstructors ?? [],
            //(Enumerator, _) => null,
            //(Async, _) => null,
            _ => []
        };
    }

    public IEnumerable<IMethodSymbol> GetCandidateMethods()
    {
        var @this = this;

        return this.GetCandidateMethods(@this.TargetMethodType ?? Normal, @this.ArgumentTypes);
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
                case HarmonyConstants.Parameter_declaringType:
                    patchData = patchData with
                    {
                        TargetType = patchAttribute.ConstructorArguments[i].Value as INamedTypeSymbol,
                        HarmonyPatchAttributes = patchData.HarmonyPatchAttributes.Add(patchAttribute)
                    };
                    break;
                case HarmonyConstants.Parameter_methodName:
                    patchData = patchData with
                    {
                        TargetMethodName = patchAttribute.ConstructorArguments[i].Value as string,
                        HarmonyPatchAttributes = patchData.HarmonyPatchAttributes.Add(patchAttribute)
                    };
                    break;
                case HarmonyConstants.Parameter_methodType:
                    var value = patchAttribute.ConstructorArguments[i].Value as int?;
                    patchData = patchData with
                    {
                        TargetMethodType = value is not null ? (HarmonyConstants.PatchTargetMethodType)value.Value : null,
                        HarmonyPatchAttributes = patchData.HarmonyPatchAttributes.Add(patchAttribute)
                    };
                    break;
                case HarmonyConstants.Parameter_argumentTypes:
                    patchData = patchData with
                    {
                        ArgumentTypes = patchAttribute.ConstructorArguments[i].Values
                            .Select(c => c.Value as INamedTypeSymbol)
                            .NotNull()
                            .ToImmutableArray(),
                        HarmonyPatchAttributes = patchData.HarmonyPatchAttributes.Add(patchAttribute)
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

    internal DiagnosticBuilder CreateDiagnosticBuilder(DiagnosticDescriptor descriptor)
    {
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(nameof(this.PatchClass), this.PatchClass.GetFullMetadataName())
            .Add(nameof(this.PatchMethod), this.PatchMethod.GetFullMetadataName())
            .Add(nameof(this.PatchType), this.PatchType?.ToString())
            .Add(nameof(this.TargetType), this.TargetType?.GetFullMetadataName())
            .Add(nameof(this.TargetMethod), this.TargetMethod?.MetadataName)
            .Add(nameof(this.TargetMethodType), this.TargetMethodType?.ToString())
            .Add(nameof(this.ArgumentTypes), string.Join(",", (this.ArgumentTypes ?? []).Select(t => t.GetFullMetadataName())));

        return new DiagnosticBuilder(descriptor)
        {
            Properties = properties
        };
    }

    internal DiagnosticBuilder CreateDiagnosticBuilder(
        DiagnosticDescriptor descriptor,
        Location primaryLocation,
        DiagnosticSeverity? severity = null,
        ImmutableArray<Location> additionalLocations = default,
        Func<ImmutableDictionary<string, string?>, ImmutableDictionary<string, string?>>? additionalProperties = null,
        ImmutableArray<object?> messageArgs = default)
    {
        if (messageArgs.IsDefault)
            messageArgs = [];

        var diagnostic = this.CreateDiagnosticBuilder(descriptor);

        var properties = diagnostic.Properties;

        properties = additionalProperties?.Invoke(properties) ?? properties;

        return diagnostic with
        {
            PrimaryLocation = primaryLocation,
            EffectiveSeverity = severity,
            AdditionalLocations = additionalLocations.IsDefault ? [] : additionalLocations,
            Properties = properties,
            MessageArgs = messageArgs.IsDefault ? [] : messageArgs
        };
    }

    internal ImmutableArray<Diagnostic> CreateDiagnostics(
        DiagnosticDescriptor descriptor,
        ImmutableArray<Location> primaryLocations = default,
        DiagnosticSeverity? severity = null,
        ImmutableArray<Location> additionalLocations = default,
        Func<ImmutableDictionary<string, string?>, ImmutableDictionary<string, string?>>? additionalProperties = null,
        ImmutableArray<object?> messageArgs = default)
    {
        if (primaryLocations.IsDefaultOrEmpty)
            primaryLocations = this.PatchMethod.Locations;

        var @this = this;
        
        return primaryLocations
            .Select(location => @this.CreateDiagnosticBuilder(descriptor, location, severity, additionalLocations, additionalProperties, messageArgs))
            .CreateAll();

        //return Diagnostic.Create(
        //    descriptor: descriptor,
        //    location: locations[0],
        //    additionalLocations: locations.Skip(1),
        //    properties: properties,
        //    messageArgs: messageArgs.ToArray());
    }
}
