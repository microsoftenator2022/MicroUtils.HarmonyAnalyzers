using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace HarmonyAnalyzers.Rules;
internal static class TargetMethodMatchFailed
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA005",
        "HarmonyPatch target method not found",
        "Target method resolution failed for patch method '{0}'. No matching method was found.",
        nameof(Constants.RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);
}
