using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class InvalidInjectedParameterType
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA014),
        "Invalid injected parameter type",
        "Invalid type '{0}' for injected parameter '{1}'. Expected '{2}'.",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static ImmutableArray<Diagnostic> Check(PatchMethodData methodData)
    {
        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Transpiler or HarmonyConstants.HarmonyPatchType.ReversePatch)
            return [];

        return methodData.PatchMethod.Parameters
            .Select(p => (param: p, expected: HarmonyHelpers.GetInjectionParameterType(p.Name, methodData)))
            .Where(p => p.expected is { } s && !methodData.Compilation.ClassifyConversion(s, p.param.Type).IsStandardImplicit())
            .SelectMany(p => methodData.CreateDiagnostics(Descriptor, p.param.Locations, messageArgs: [p.param.Type, p.param.Name, p.expected]))
            .ToImmutableArray();
    }
}
