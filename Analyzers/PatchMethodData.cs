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
    Compilation Compilation,
    HarmonyConstants.HarmonyPatchType? PatchType = null,
    INamedTypeSymbol? TargetType = null,
    string? TargetMethodName = null,
    HarmonyConstants.PatchTargetMethodType? TargetMethodType = null,
    ImmutableArray<ITypeSymbol>? ArgumentTypes = null)
{
    public ImmutableArray<AttributeData> HarmonyPatchAttributes { get; init; } = [];

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

    public IEnumerable<TSymbol> GetAllTargetTypeMembers<TSymbol>() where TSymbol : ISymbol =>
        this.TargetType?.GetMembers().OfType<TSymbol>() ?? [];

    public IEnumerable<TSymbol> GetCandidateTargetMembers<TSymbol>() where TSymbol : ISymbol
    {
        var @this = this;
        return @this.GetAllTargetTypeMembers<TSymbol>().Where(s => s.Name == @this.TargetMethodName);
    }

    private IEnumerable<IPropertySymbol> GetTargetProperties()
    {
        var @this = this;
        return @this.GetAllTargetTypeMembers<IPropertySymbol>()
            .Where(s => string.IsNullOrEmpty(@this.TargetMethodName) ? s.IsIndexer : s.Name == @this.TargetMethodName);
    }

    public IEnumerable<IMethodSymbol> GetCandidateMethods(
        HarmonyConstants.PatchTargetMethodType targetMethodType,
        IEnumerable<ITypeSymbol>? argumentTypes)
    {
        var @this = this;

        return (targetMethodType, argumentTypes) switch
        {
            (Getter, null) => @this.GetTargetProperties().Choose(p => Optional.MaybeValue(p.GetMethod)),
            (Getter, _) => @this.GetTargetProperties().Choose(p => Optional.MaybeValue(p.GetMethod)).FindMethodsWithArgs(argumentTypes, this.Compilation),
            (Setter, null) => @this.GetTargetProperties().Choose(p => Optional.MaybeValue(p.SetMethod)),
            (Setter, _) => @this.GetTargetProperties().Choose(p => Optional.MaybeValue(p.SetMethod)).FindMethodsWithArgs(argumentTypes, this.Compilation),
            (Constructor, null) => @this.TargetType?.Constructors.Where(m => !m.IsStatic) ?? [],
            (Constructor, _) => @this.TargetType?.Constructors.Where(m => !m.IsStatic).FindMethodsWithArgs(argumentTypes, this.Compilation) ?? [],
            (StaticConstructor, _) => @this.TargetType?.StaticConstructors ?? [],
            (Enumerator, _) => @this.GetCandidateMethods(Normal, null)
                .Choose(m => Optional.MaybeValue(Util.GetEnumeratorMoveNext(m, @this.Compilation))),
            //(Async, _) => null,
            (_, null) => @this.GetCandidateTargetMembers<IMethodSymbol>(),
            (_, _) => @this.GetCandidateTargetMembers<IMethodSymbol>().FindMethodsWithArgs(argumentTypes, this.Compilation)
        };
    }

    public IEnumerable<IMethodSymbol> GetCandidateMethods()
    {
        var @this = this;

        return this.GetCandidateMethods(@this.TargetMethodType ?? Normal, @this.ArgumentTypes);
    }

    private class LazyWrapper<T>()
    {
        private Lazy<T>? LazyValue;

        public T Value => this.LazyValue!.Value;

        public bool HasValue => this.LazyValue is not null;

        public bool SetValueThunk(Func<T> func)
        {
            if (this.LazyValue is not null)
                return false;

            this.LazyValue = new(func);

            return true;
        }

        public T GetValueOr(Func<T> thunk)
        {
            _ = this.SetValueThunk(thunk);

            return this.LazyValue!.Value;
        }
    }

    private readonly LazyWrapper<IMethodSymbol?> LazyTargetMethod = new();

    public IMethodSymbol? TargetMethod
    {
        get
        {
            var @this = this;

            return this.LazyTargetMethod.GetValueOr(() =>
                @this.GetCandidateMethods()
                    .TrySingle()
                    .ValueOrDefault());
        }
    }

    /// <summary>
    /// Classes and/or interfaces that can be used to access the object instance.
    /// This may not include the class containing the method (ie. <see cref="ISymbol.CanBeReferencedByName"/> is false).
    /// Returns an empty <see cref="ImmutableArray{}"/> if the method is static
    /// or if the target method cannot be resolved for this <see cref="PatchMethodData"/> instance.
    /// </summary>
    public ImmutableArray<INamedTypeSymbol> TargetMethodInstanceTypes
    {
        get
        {
            if (this.TargetMethod is null)
                return [];

            if (this.TargetMethod.IsStatic)
                return [];

            return this.TargetMethod.ContainingType.GetNameAccessBaseTypes();
        }
    }

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
                            .Choose(c => Optional.MaybeValue(c.Value as ITypeSymbol))
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
            .Distinct()
            .Select(location => @this.CreateDiagnosticBuilder(descriptor, location, severity, additionalLocations, additionalProperties, messageArgs))
            .CreateAll();
    }

    public static Optional<PatchMethodData> TryCreate(
        IMethodSymbol method,
        Compilation compilation,
        INamedTypeSymbol? harmonyPatchAttributeType = default,
        ImmutableArray<(HarmonyConstants.HarmonyPatchType, INamedTypeSymbol)> harmonyPatchTypeAttributeTypes = default,
        CancellationToken ct = default)
    {
        harmonyPatchAttributeType ??= HarmonyHelpers.GetHarmonyPatchType(compilation, ct);

        if (harmonyPatchTypeAttributeTypes.IsDefaultOrEmpty)
            harmonyPatchTypeAttributeTypes = HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(compilation, ct).ToImmutableArray();

        if (harmonyPatchAttributeType is null ||
            harmonyPatchTypeAttributeTypes.Length < Enum.GetValues(typeof(HarmonyConstants.HarmonyPatchType)).Length)
            return default;

        if (!HarmonyHelpers.MayBeHarmonyPatchMethod(
                method,
                harmonyPatchAttributeType,
                harmonyPatchTypeAttributeTypes.Select(tuple => tuple.Item2)))
            return default;

        var methodData = new PatchMethodData(method.ContainingType, method, compilation);

        var methodAttributes = method.GetAttributes().ToImmutableArray();

        var targetMethodAttributes = methodData.PatchClass.GetAttributes()
            .Concat(methodAttributes)
            .Where(attr => harmonyPatchAttributeType.Equals(attr.AttributeClass, SymbolEqualityComparer.Default));

        foreach (var attr in targetMethodAttributes)
        {
            methodData = methodData.AddTargetMethodData(attr);
        }

        var patchType = HarmonyHelpers.TryParseHarmonyPatchType(method.Name);

        if (!patchType.HasValue)
            patchType = harmonyPatchTypeAttributeTypes.TryPick(tuple =>
                methodAttributes
                    .Select(attr => attr.AttributeClass)
                        .Contains(tuple.Item2, SymbolEqualityComparer.Default) ?
                        tuple.Item1 :
                        Optional.NoValue<HarmonyConstants.HarmonyPatchType>());

        if (patchType.HasValue)
            methodData = methodData with { PatchType = patchType.Value };

        return methodData;
    }
}
