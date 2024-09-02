using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

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

    private static void Report(
        SyntaxNodeAnalysisContext context, 
        PatchMethodData patchMethodData,
        ImmutableArray<Location> locations,
        object?[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: Descriptor,
            location: locations[0],
            additionalLocations: locations.Skip(1),
            messageArgs: messageArgs.Append(patchMethodData.PatchMethod).ToArray()));

    internal static bool Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData patchMethodData)
    {
        if (patchMethodData.TargetMethodType is not null)
            return false;

        var locations = patchMethodData.PatchMethod.Locations;

        if (patchMethodData.GetCandidateTargetMembers<IPropertySymbol>().FirstOrDefault() is { } property)
        {

            if (property.GetMethod is { } getter)
            {
                Report(
                    context,
                    patchMethodData,
                    locations,
                    [HarmonyConstants.PatchTargetMethodType.Getter, getter]);
            }

            if (property.SetMethod is { } setter)
            {
                Report(
                    context,
                    patchMethodData,
                    locations,
                    [HarmonyConstants.PatchTargetMethodType.Setter, setter]);
            }

            return true;
        }

        if (patchMethodData.TargetMethod is null)
        {
            if (patchMethodData.ArgumentTypes is not null &&
                patchMethodData.GetCandidateMethods(HarmonyConstants.PatchTargetMethodType.Constructor, patchMethodData.ArgumentTypes).FirstOrDefault() is { } constructor)
            {
                Report(
                    context,
                    patchMethodData,
                    locations,
                    [HarmonyConstants.PatchTargetMethodType.Constructor, constructor]);
            }
            else if (patchMethodData.ArgumentTypes is { } args &&
                patchMethodData.GetAllTargetTypeMembers<IPropertySymbol>().FirstOrDefault(p => p.IsIndexer) is { } indexer)
            {
                if (indexer.GetMethod is { } getter)
                {
                    Report(
                        context,
                        patchMethodData,
                        locations,
                        [HarmonyConstants.PatchTargetMethodType.Getter, getter]);
                }

                if (indexer.SetMethod is { } setter)
                {
                    Report(
                        context,
                        patchMethodData,
                        locations,
                        [HarmonyConstants.PatchTargetMethodType.Setter, setter]);
                }
            }
            else return false;

            return true;
        }

        return false;
    }
}
