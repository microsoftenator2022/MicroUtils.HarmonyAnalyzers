using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

class PatchMethodParamterNotFoundOnTargetMethod
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA013",
        "Patch method parameter does not match any target method parameter",
        "Parameter '{0}' does not match any parameter on target method '{1}'",
        nameof(Constants.RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(SyntaxNodeAnalysisContext context, PatchMethodData methodData)
    {
        if (methodData.TargetMethod is null)
            return;

        foreach (var p in methodData.PatchMethod.Parameters)
        {
            if (!methodData.TargetMethod.Parameters.Any(tp => tp.Name == p.Name && context.Compilation.ClassifyConversion(tp.Type, p.Type).IsImplicit))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: Descriptor,
                    location: p.Locations[0],
                    additionalLocations: p.Locations.Skip(1),
                    messageArgs: [p, methodData.TargetMethod]));
            }
        }
    }
}
