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

internal readonly struct NoPatchMethods : IPatchClassRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA006),
        "No patch methods in patch class",
        "Patch class contains no patch methods",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchClassData patchClassData,
        CancellationToken _)
    {
        if (patchClassData.ClassAttributes.Length > 0 && patchClassData.PatchMethods.Length == 0)
        { 
            return new DiagnosticBuilder(Descriptor).ForAllLocations(patchClassData.ClassSymbol.Locations).CreateAll().ToImmutableArray();
        }

        return [];
    }
}
