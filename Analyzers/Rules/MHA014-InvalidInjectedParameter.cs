using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class InvalidInjectedParameter
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA014),
        "Invalid injected parameter type",
        "Invalid type '{0}' for injected parameter '{1}'",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static ImmutableArray<Diagnostic> Check(
        Compilation compilation,
        PatchMethodData methodData)
    {
        return methodData.PatchMethod.Parameters
            .Where(p => HarmonyHelpers.GetInjectionParameterType(p.Name, compilation, methodData.TargetMethod) is { } s &&
                !compilation.ClassifyConversion(s, p.Type).IsStandardImplicit())
            .SelectMany(p => methodData.CreateDiagnostics(Descriptor, p.Locations, messageArgs: [p.Type, p]))
            .ToImmutableArray();
    }
}
