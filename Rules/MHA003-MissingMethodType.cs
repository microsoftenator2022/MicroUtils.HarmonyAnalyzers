using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal static class MissingMethodType
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA003",
        "Missing MethodType argument for HarmonyPatch attribute",
        "Cannot find target method for patch method '{0}', but a matching method for " + Constants.Type_HarmonyLib_MethodType + ".{1} was found",
        nameof(Constants.RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static bool Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData patchMethodData,
        ImmutableArray<Location> locations)
    {
        if (patchMethodData.TargetMethodType is not null ||
            patchMethodData.GetCandidateTargetMembers<IPropertySymbol>().FirstOrDefault() is not { } property)
        {
            return false;
        }

        void report(Constants.PatchTargetMethodType methodType) =>
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: locations[0],
                additionalLocations: locations.Skip(1),
                messageArgs: [patchMethodData.PatchMethod, methodType]));

        if (property.GetMethod is not null)
            report(Constants.PatchTargetMethodType.Getter);

        if (property.SetMethod is not null)
            report(Constants.PatchTargetMethodType.Setter);

        return true;
    }
}
