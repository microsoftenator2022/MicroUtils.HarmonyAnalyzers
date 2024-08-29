using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HarmonyAnalyzers.Rules;
internal static class NoPatchMethods
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA006",
        "No patch methods in patch class",
        "Patch class {0} contains no patch methods",
        nameof(Constants.RuleCategory.MissingAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classAttributes,
        ImmutableArray<PatchMethodData> methodAttributes)
    {
        if (classAttributes.Length > 0 && methodAttributes.Length == 0)
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: classSymbol.Locations[0],
                additionalLocations: classSymbol.Locations.Skip(1),
                messageArgs: [classSymbol]));
    }
}
