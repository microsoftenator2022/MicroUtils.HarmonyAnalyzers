﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class MissingClassAttribute
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA001),
        $"Missing {HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch} class attribute",
        "Class '{0}' lacks a '{1}' attribute, but has one or more members with a '{1}' attribute",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classPatchAttributes,
        ImmutableArray<PatchMethodData> methodAttributes,
        INamedTypeSymbol harmonyPatchAttributeType)
    {
        if (classPatchAttributes.Length == 0 && methodAttributes.Length > 0)
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: classSymbol.Locations[0],
                additionalLocations: classSymbol.Locations.Skip(1),
                messageArgs: [classSymbol, harmonyPatchAttributeType]));
    }
}
