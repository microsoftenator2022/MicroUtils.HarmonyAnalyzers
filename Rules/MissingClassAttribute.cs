using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HarmonyAnalyzers.Rules;
internal static class MissingClassAttribute
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA001",
        $"Missing {Constants.Attribute_HarmonyLib_HarmonyPatch} class attribute",
        "Class '{0}' lacks a " + Constants.Attribute_HarmonyLib_HarmonyPatch + " attribute, but has one or more members with a " + Constants.Attribute_HarmonyLib_HarmonyPatch + " attribute",
        nameof(Constants.RuleCategory.MissingAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classAttributes,
        ImmutableArray<PatchMethodData> methodAttributes)
    {
        if (classAttributes.Length == 0 && methodAttributes.Length > 0)
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: classSymbol.Locations[0],
                additionalLocations: classSymbol.Locations.Skip(1),
                messageArgs: [classSymbol]));
    }
}
