using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class PatchTypeAttributeConflict
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA011),
        "Patch type conflict",
        "Patch type attribute conflict",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static IEnumerable<Location> CheckInternal(
        Compilation compilation,
        PatchMethodData methodData,
        //ImmutableArray<INamedTypeSymbol> patchTypeAttributeTypes,
        CancellationToken ct)
    {
        if (methodData.PatchType is null)
            return [];

        HarmonyConstants.HarmonyPatchType? methodNamePatchType = null;
        
        if (HarmonyHelpers.TryParseHarmonyPatchType(methodData.PatchMethod.Name, out var patchType))
            methodNamePatchType = patchType;

        
        var patchTypeAttributes =
            methodData.GetPatchTypeAttributes(compilation, ct)
                .ToImmutableArray();

        if (patchTypeAttributes.Length == 0)
            return [];

        if (patchTypeAttributes.Length == 1 &&
            patchTypeAttributes[0].Item2 ==
                (methodNamePatchType is not null ? methodNamePatchType : methodData.PatchType))
                return [];

        return patchTypeAttributes
            .Select(pair => pair.Item1.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            .NotNull()
            .Concat(methodNamePatchType is not null ? methodData.PatchMethod.Locations : []);
            
        //var methodPatchTypeAttributes = methodData.PatchMethod.GetAttributes()
        //    .Where(attr => patchTypeAttributeTypes.Any(pta => pta.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)))
        //    .ToImmutableArray();

        //var methodPatchTypeAttributeTypes = methodPatchTypeAttributes
        //    .Select(attr => attr.AttributeClass)
        //    .Distinct(SymbolEqualityComparer.Default)
        //    .NotNull()
        //    .ToImmutableArray();

        //HarmonyConstants.HarmonyPatchType? methodNamePatchType = null;

        //if (Enum.TryParse<HarmonyConstants.HarmonyPatchType>(methodData.PatchMethod.Name, out var patchType) &&
        //    patchType.GetPatchTypeAttributeType(compilation, ct) is { } patchTypeAttributeType)
        //    methodNamePatchType = patchType;
        //else
        //    patchTypeAttributeType = null;

        //if (methodPatchTypeAttributeTypes.Length > 1 ||
        //    (methodNamePatchType is not null && methodPatchTypeAttributeTypes.Length > 0))
        //{
        //    foreach (var attr in methodPatchTypeAttributes)
        //    {
        //        if (ct.IsCancellationRequested)
        //            yield break;

                
        //    }
        //}
    }

    internal static ImmutableArray<Diagnostic> Check(
        Compilation compilation,
        PatchMethodData methodData,
        //INamedTypeSymbol patchTypeAttributeType,
        CancellationToken ct)
    {
        //if (methodData.PatchType is { } patchType &&
        //    methodData.PatchMethod.GetAttributes()
        //        .Select(attr => attr.AttributeClass)
        //        .NotNull()
        //        .Any(t => patchTypeAttributeType.Equals(t, SymbolEqualityComparer.Default) &&
        //            !t.Equals(HarmonyHelpers.GetPatchTypeAttributeType(patchType, compilation, ct), SymbolEqualityComparer.Default)))
        //{
        var conflictLocations = CheckInternal(compilation, methodData, ct).ToImmutableArray();
        if (conflictLocations.Length > 0)
            return methodData.CreateDiagnostics(Descriptor, conflictLocations);

        //context.ReportDiagnostic(methodData.CreateDiagnostic(
        //    descriptor: Descriptor,
        //    locations: [methodData.PatchMethod.Locations[0]],
        //    messageArgs: [patchTypeAttributeType]));

        //return true;
        //}

        return [];
    }
}
