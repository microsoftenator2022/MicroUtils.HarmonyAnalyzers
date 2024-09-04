using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal class PaasthroughPostfixResultInjection
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA012),
        "__result injection in passthrough Postfix",
        "Unnecessary injected __result parameter in passthrough postfix",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Postfix)
            return;

        if (!methodData.PatchMethod.ReturnsVoid &&
            methodData.PatchMethod.Parameters.Skip(1).FirstOrDefault(p => p.Name == HarmonyConstants.Parameter_injection__result) is { } p)
        {
            context.ReportDiagnostic(methodData.CreateDiagnostic(
                descriptor: Descriptor,
                locations: [p.Locations[0]]));
        }
    }
}
