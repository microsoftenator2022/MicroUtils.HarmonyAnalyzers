using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

class PatchMethodParamterNotFoundOnTargetMethod
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA013),
        "Patch method parameter does not match target",
        "Parameter '{0}' does not match {1}",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(SyntaxNodeAnalysisContext context, PatchMethodData methodData)
    {
        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Transpiler || methodData.TargetMethod is null)
            return;

        foreach (var p in methodData.PatchMethod.Parameters
            .Skip(methodData.IsPassthroughPostfix ? 1 : 0))
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            if (HarmonyHelpers.IsInjectionNameConstant(p.Name))
                continue;

            if (p.Name.StartsWith("___") && methodData.TargetType is not null)
            {
                var fieldInjectionMatch = HarmonyHelpers.FieldInjectionRegex.Match(p.Name);
                if (fieldInjectionMatch.Success &&
                    methodData.TargetType.GetMembers().OfType<IFieldSymbol>().Any(f =>
                        f.Name == fieldInjectionMatch.Groups[1].Value &&
                        context.Compilation.ClassifyConversion(f.Type, p.Type).IsImplicit))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: Descriptor,
                    location: p.Locations[0],
                    additionalLocations: p.Locations.Skip(1),
                    messageArgs: [p, $"any field for target type {methodData.TargetType}"]));

                continue;
            }

            var argInjectionMatch = HarmonyHelpers.ArgInjectionRegex.Match(p.Name);

            if (methodData.TargetMethod.Parameters.Indexed().Any(tp =>
                (tp.element.Name == p.Name || (argInjectionMatch.Success && tp.index == int.Parse(argInjectionMatch.Groups[1].Value))) &&
                context.Compilation.ClassifyConversion(tp.element.Type, p.Type).IsImplicit))
                continue;

            context.ReportDiagnostic(methodData.CreateDiagnostic(
                descriptor: Descriptor,
                locations: p.Locations,
                messageArgs: [p, $"any parameter for target method {methodData.TargetMethod}"]));
        }
    }
}
