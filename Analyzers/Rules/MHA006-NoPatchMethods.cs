using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class NoPatchMethods
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA006),
        "No patch methods in patch class",
        "Patch class contains no patch methods",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static ImmutableArray<Diagnostic> Check(
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classAttributes,
        ImmutableArray<PatchMethodData> methodAttributes)
    {
        if (classAttributes.Length > 0 && methodAttributes.Length == 0)
        { 
            return new DiagnosticBuilder(Descriptor).ForAllLocations(classSymbol.Locations).CreateAll().ToImmutableArray();
        }

        return [];
    }
}
