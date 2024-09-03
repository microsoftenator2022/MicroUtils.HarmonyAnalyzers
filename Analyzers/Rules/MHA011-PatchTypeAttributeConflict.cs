using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal class PatchTypeAttributeConflict
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA011),
        "Patch type conflict",
        "Patch method has conflicting patch type attribute {0}",
        nameof(RuleCategory.PatchMethod),
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
            context.ReportDiagnostic(methodData.CreateDiagnostic(
                descriptor: Descriptor,
                locations: methodData.PatchMethod.Locations,
                messageArgs: [patchTypeAttributeType]));

            return true;
        }

        return false;
    }
}
