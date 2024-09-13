using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class ReversePatchType
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA018),
        "Reverse patch signature does not match target method",
        "Reverse patch method '{0}' signature does not match target method '{1}'. Expected '{2}'.",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static ImmutableArray<Diagnostic> Check(PatchMethodData methodData, CancellationToken ct)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.ReversePatch ||
            methodData.TargetMethod is not { } targetMethod)
            return [];

        var IEnumerableCodeInstructionType = HarmonyHelpers.GetIEnumerableCodeInstructionType(methodData.Compilation, ct);

        var syntaxReferences = methodData.PatchMethod.DeclaringSyntaxReferences;

        // Try to find transpiler local function
        // Reverse transpiler method signature may differ from original
        foreach (var methodSyntax in syntaxReferences.Select(sr => sr.GetSyntax()).OfType<MethodDeclarationSyntax>())
        {
            if (ct.IsCancellationRequested)
                return [];

            var sm = methodData.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);

            if (methodSyntax.Body is { } body)
            {
                foreach (var lf in body.DescendantNodes(_ => true).OfType<LocalFunctionStatementSyntax>())
                {
                    if (ct.IsCancellationRequested)
                        return [];

                    if (IEnumerableCodeInstructionType is not null && 
                        sm.GetDeclaredSymbol(lf) is IMethodSymbol localMethodSymbol &&
                        methodData.Compilation.ClassifyConversion(localMethodSymbol.ReturnType, IEnumerableCodeInstructionType).IsStandardImplicit())
                        return [];
                }

            }
        }

        IEnumerable<ITypeSymbol> getExpectedTypes()
        {
            if (!targetMethod.IsStatic)
                yield return targetMethod.ContainingType;

            foreach (var m in targetMethod.Parameters.Select(p => p.Type))
                yield return m;
        }

        var expectedTypes = getExpectedTypes().ToImmutableArray();

        if (methodData.PatchMethod.Parameters.Length == expectedTypes.Length &&
            methodData.PatchMethod.ReturnType.Equals(methodData.TargetMethod.ReturnType, SymbolEqualityComparer.Default) &&
            methodData.PatchMethod.Parameters
                .Select(p => p.Type)
                .Zip(expectedTypes, (p1, p2) => (p1, p2))
                .All(ps => ps.p1.Equals(ps.p2, SymbolEqualityComparer.Default)))
            return [];

        return methodData.CreateDiagnostics(
            Descriptor,
            messageArgs:
            [
                methodData.PatchMethod,
                methodData.TargetMethod,
                $"{methodData.TargetMethod.ReturnType} {methodData.PatchMethod.Name}({string.Join(", ", expectedTypes)})"
            ]);
    }
}
