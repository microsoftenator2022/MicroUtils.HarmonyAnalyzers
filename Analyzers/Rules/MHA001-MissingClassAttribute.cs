using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct MissingClassAttribute : IPatchClassRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA001),
        $"Missing {HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch} class attribute",
        "Class '{0}' lacks a '{1}' attribute, but has one or more members with a '{1}' attribute",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchClassData patchClassData,
        //INamedTypeSymbol harmonyPatchAttributeType,
        CancellationToken _)
    {
        if (patchClassData.ClassAttributes.Length == 0 && patchClassData.PatchMethods.Length > 0)
        {
            var diagnostic = new DiagnosticBuilder(Descriptor)
            {
                MessageArgs = [patchClassData.ClassSymbol, patchClassData.CommonSymbols.HarmonyPatchAttribute]
            };

            return diagnostic.ForAllLocations(patchClassData.ClassSymbol.Locations).CreateAll().ToImmutableArray();
        }

        return [];
    }
}
