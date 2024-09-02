using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class TargetMethodMatchFailed
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA005),
        "HarmonyPatch target method not found",
        "Patch target method resolution failed. No matching method was found.",
        nameof(RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);
}
