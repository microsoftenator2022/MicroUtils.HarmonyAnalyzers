using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class InjectedParamterNotFoundOnTargetMethod
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA013),
        "Patch method parameter does not match target",
        "Parameter '{0}' does not match {1}",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    private static IEnumerable<Diagnostic> CheckInternal(
        PatchMethodData methodData,
        CancellationToken ct)
    {
        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Transpiler or HarmonyConstants.HarmonyPatchType.ReversePatch || methodData.TargetMethod is null)
            yield break;

        foreach (var p in methodData.PatchMethod.Parameters
            .Skip(methodData.PatchMethod.MayBePassthroughPostfix(methodData.TargetMethod, methodData.Compilation) ? 1 : 0))
        {
            if (ct.IsCancellationRequested)
                yield break;

            if (HarmonyHelpers.IsInjectionNameConstant(p.Name))
                continue;

            if (p.Name.StartsWith("___") && methodData.TargetType is not null)
            {
                var fieldInjectionMatch = HarmonyHelpers.FieldInjectionRegex.Match(p.Name);
                if (fieldInjectionMatch.Success &&
                    methodData.TargetType.GetMembers().OfType<IFieldSymbol>().Any(f =>
                        f.Name == fieldInjectionMatch.Groups[1].Value &&
                        methodData.Compilation.ClassifyConversion(f.Type, p.Type).IsStandardImplicit()))
                    continue;

                foreach (var d in methodData.CreateDiagnostics(
                    Descriptor, p.Locations, messageArgs: [p, $"any field for target type {methodData.TargetType}"]))
                    yield return d;

                continue;
            }

            var argInjectionMatch = HarmonyHelpers.ArgInjectionRegex.Match(p.Name);

            if (methodData.TargetMethod.Parameters.Indexed().Any(tp =>
                (tp.element.Name == p.Name || (argInjectionMatch.Success && tp.index == int.Parse(argInjectionMatch.Groups[1].Value))) &&
                methodData.Compilation.ClassifyConversion(tp.element.Type, p.Type).IsStandardImplicit()))
                continue;

            foreach (var d in methodData.CreateDiagnostics(
                Descriptor,
                p.Locations,
                messageArgs:
                [
                    p,
                    $"any parameter for target method {methodData.TargetMethod.ToDisplayString(
                        SymbolDisplayFormat.CSharpShortErrorMessageFormat.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeParameters))}" +
                    $"({string.Join(", ", methodData.TargetMethod.Parameters)})"
                ]))
                yield return d;
        }
    }

    internal static ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        CancellationToken ct) => CheckInternal(methodData, ct).ToImmutableArray();
}
