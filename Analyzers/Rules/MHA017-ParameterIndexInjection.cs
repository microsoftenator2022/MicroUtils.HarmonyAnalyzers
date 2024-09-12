using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class ParameterIndexInjection
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA017),
        "Use parameter name over index injection (__n)",
        "Use parameter name {1}over parameter index injection '{0}'",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        true);

    private static IEnumerable<IEnumerable<Diagnostic>> CheckInternal(PatchMethodData methodData)
    {
        var argInjections = methodData.PatchMethod.Parameters
            .Select(p => (p, HarmonyHelpers.ArgInjectionRegex.Match(p.Name)))
            .Where(p => p.Item2.Success)
            .Select(p => (p.p, int.Parse(p.Item2.Groups[1].Value)));

        if (!argInjections.Any())
            yield break;

        foreach (var (p, index) in argInjections)
        {
            var parameterName = methodData.TargetMethod?.Parameters[index].Name;

            yield return methodData.CreateDiagnostics(
                Descriptor,
                p.Locations,
                additionalProperties: props => props.Add("ParameterName", parameterName),
                messageArgs: [p.Name, (parameterName is not null ? $"'{parameterName}' " : "")]);
        }
    }

    internal static ImmutableArray<Diagnostic> Check(PatchMethodData methodData) =>
        CheckInternal(methodData).Concat().ToImmutableArray();
}
