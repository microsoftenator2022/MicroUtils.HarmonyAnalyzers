using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal class PaasthroughPostfixResultInjection
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA012",
        "__result injection in passthrough Postfix",
        "Unnecessary injected __result parameter in passthrough postfix",
        nameof(Constants.RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        if (methodData.PatchType is not Constants.HarmonyPatchType.Postfix)
            return;

        if (!methodData.PatchMethod.ReturnsVoid &&
            methodData.PatchMethod.Parameters.Skip(1).FirstOrDefault(p => p.Name == Constants.Parameter_injection_result) is { } p)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: p.Locations[0],
                additionalLocations: p.Locations.Skip(1)));
        }
    }
}
