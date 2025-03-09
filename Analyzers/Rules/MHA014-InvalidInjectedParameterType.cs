using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct InvalidInjectedParameterType : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA014),
        "Invalid injected parameter type",
        "Invalid type '{0}' for injected parameter '{1}'. Expected '{2}'.",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken ct)
    {
        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Transpiler or HarmonyConstants.HarmonyPatchType.ReversePatch)
            return [];

        return methodData.PatchMethod.Parameters
            .Select(p => (param: p, expected: HarmonyHelpers.GetInjectionParameterType(p.Name, methodData)))
            .Where(p => p.expected is { } s && !methodData.Compilation.ClassifyConversion(s, p.param.Type).IsStandardImplicit())
            .SelectMany(p =>
            {
                var locations =
                    
                    p.param.DeclaringSyntaxReferences
                        .Select(sr => sr.GetSyntax(ct))
                        .OfType<ParameterSyntax>()
                        .Select(p => p.Type?.GetLocation() ?? p.GetLocation())
                        .ToImmutableArray();

                if (locations.IsDefaultOrEmpty)
                    locations = p.param.Locations;

                return methodData.CreateDiagnostics(Descriptor, locations, messageArgs: [p.param.Type, p.param.Name, p.expected]);
            })
            .ToImmutableArray();
    }
}
