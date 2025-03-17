using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct ParameterIndexInjection : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA017),
        "Use parameter name over index injection (__n)",
        "Use parameter name {1}over parameter index injection '{0}'",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    private static IEnumerable<IEnumerable<Diagnostic>> CheckInternal(
        PatchMethodData methodData,
        CancellationToken ct)
    {
        var argInjections = methodData.PatchMethod.Parameters
            .Select(p => (p, HarmonyHelpers.ArgInjectionRegex.Match(p.Name)))
            .Where(p => p.Item2.Success)
            .Select(p => (p.p, int.Parse(p.Item2.Groups[1].Value)));

        if (!argInjections.Any())
            yield break;

        foreach (var (p, index) in argInjections)
        {
            if (ct.IsCancellationRequested)
                yield break;

            var parameterName = methodData.TargetMethod?.Parameters[index].Name;

            yield return methodData.CreateDiagnostics(
                Descriptor,
                p.Locations,
                additionalProperties: props => props.Add("ParameterName", parameterName),
                messageArgs: [p.Name, (parameterName is not null ? $"'{parameterName}' " : "")]);
        }
    }

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken cancellationToken) =>
        CheckInternal(methodData, cancellationToken).Concat().ToImmutableArray();
}
