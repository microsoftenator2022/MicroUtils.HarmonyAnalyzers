using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class MissingPatchTypeAttribute
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA002),
        $"Missing Harmony patch type method attribute",
        "Patch method requires a Harmony patch type attribute",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static ImmutableArray<Diagnostic> Check(
        //SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        if (methodData.PatchType is null)
        {
            return methodData.CreateDiagnostics(Descriptor);

            //context.ReportDiagnostic(methodData.CreateDiagnostic(descriptor: Descriptor));
        }

        return [];
    }
}
