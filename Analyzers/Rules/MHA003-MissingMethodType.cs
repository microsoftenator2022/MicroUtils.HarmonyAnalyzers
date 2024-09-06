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

internal static class MissingMethodType
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA003),
        "Missing MethodType argument for HarmonyPatch attribute",
        "Cannot find target method for patch method '{2}', but a matching {0} method '{1}' was found",
        nameof(RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    private static IEnumerable<Diagnostic> Report(
        //SyntaxNodeAnalysisContext context, 
        PatchMethodData patchMethodData,
        HarmonyConstants.PatchTargetMethodType methodType,
        IMethodSymbol methodSymbol)
    {
        return patchMethodData.CreateDiagnostics(
            descriptor: Descriptor,
            additionalProperties: properties => properties
                .SetItem(nameof(PatchMethodData.TargetMethodType), methodType.ToString())
                .SetItem(nameof(PatchMethodData.TargetMethod), methodSymbol.MetadataName),
            messageArgs: [methodType, methodSymbol, patchMethodData.PatchMethod]);

            //return context.ReportDiagnostic(patchMethodData.CreateDiagnostic(
            //    descriptor: Descriptor,
            //    additionalProperties: dict => dict
            //        .SetItem(nameof(PatchMethodData.TargetMethodType), methodType.ToString())
            //        .SetItem(nameof(PatchMethodData.TargetMethod), methodSymbol.MetadataName),
            //    messageArgs: [methodType, methodSymbol, patchMethodData.PatchMethod]));
    }

    private static IEnumerable<IEnumerable<Diagnostic>> CheckInternal(
        //SyntaxNodeAnalysisContext context,
        PatchMethodData patchMethodData,
        CancellationToken ct)
    {
        if (patchMethodData.TargetMethodType is not null)
            yield break;

        if (patchMethodData.GetCandidateTargetMembers<IPropertySymbol>().FirstOrDefault() is { } property)
        {
            if (property.GetMethod is { } getter)
            {
                yield return Report(
                    patchMethodData,
                    HarmonyConstants.PatchTargetMethodType.Getter,
                    getter);
            }

            if (property.SetMethod is { } setter)
            {
                yield return Report(
                    patchMethodData,
                    HarmonyConstants.PatchTargetMethodType.Setter,
                    setter);
            }

            yield break;
        }

        if (ct.IsCancellationRequested)
            yield break;

        if (patchMethodData.TargetMethod is null)
        {
            if (patchMethodData.ArgumentTypes is not null &&
                patchMethodData.GetCandidateMethods(HarmonyConstants.PatchTargetMethodType.Constructor, patchMethodData.ArgumentTypes)
                    .FirstOrDefault() is { } constructor)
            {
                yield return Report(
                    patchMethodData,
                    HarmonyConstants.PatchTargetMethodType.Constructor,
                    constructor);
            }
            else if (patchMethodData.ArgumentTypes is not null &&
                patchMethodData.GetAllTargetTypeMembers<IPropertySymbol>().FirstOrDefault(p => p.IsIndexer) is { } indexer)
            {
                if (indexer.GetMethod is { } getter)
                {
                    yield return Report(
                        patchMethodData,
                        HarmonyConstants.PatchTargetMethodType.Getter,
                        getter);
                }

                if (indexer.SetMethod is { } setter)
                {
                    yield return Report(
                        patchMethodData,
                        HarmonyConstants.PatchTargetMethodType.Setter,
                        setter);
                }
            }
        }
    }

    internal static ImmutableArray<Diagnostic> Check(
        PatchMethodData patchMethodData,
        CancellationToken ct) =>
        CheckInternal(patchMethodData, ct).SelectMany(d => d).ToImmutableArray();
}
