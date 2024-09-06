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
        "Patch method has conflicting patch type attribute {0}",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static ImmutableArray<Diagnostic> Check(
        Compilation compilation,
        PatchMethodData methodData,
        INamedTypeSymbol patchTypeAttributeType,
        CancellationToken ct)
    {
        if (methodData.PatchType is { } patchType &&
            methodData.PatchMethod.GetAttributes()
                .Select(attr => attr.AttributeClass)
                .NotNull()
                .Any(t => patchTypeAttributeType.Equals(t, SymbolEqualityComparer.Default) &&
                    !t.Equals(patchType.GetPatchTypeAttributeType(compilation, ct), SymbolEqualityComparer.Default)))
        {
            return methodData.CreateDiagnostics(Descriptor, messageArgs: [patchTypeAttributeType]);

            //context.ReportDiagnostic(methodData.CreateDiagnostic(
            //    descriptor: Descriptor,
            //    locations: [methodData.PatchMethod.Locations[0]],
            //    messageArgs: [patchTypeAttributeType]));

            //return true;
        }

        return [];
    }
}
