using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal class PatchTypeAttributeConflict
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA011",
        "Patch type conflict",
        "Patch method '{0}' has conflicting patch type attribute {1}",
        nameof(Constants.RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static bool Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData,
        INamedTypeSymbol patchTypeAttributeType)
    {
        if (methodData.PatchType is { } patchType &&
            methodData.PatchMethod.GetAttributes()
                .Select(attr => attr.AttributeClass)
                .NotNull()
                .Any(t => patchTypeAttributeType.Equals(t, SymbolEqualityComparer.Default) &&
                    !t.Equals(patchType.GetPatchTypeAttributeType(context.Compilation, context.CancellationToken), SymbolEqualityComparer.Default)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: methodData.PatchMethod.Locations[0],
                additionalLocations: methodData.PatchMethod.Locations.Skip(1),
                [methodData.PatchMethod, patchTypeAttributeType]));

            return true;
        }

        return false;
    }
}
