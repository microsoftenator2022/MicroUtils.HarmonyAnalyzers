using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA018;

using static SyntaxFactory;

internal static class FixMethodSignature
{
    internal static async Task<CodeAction?> GetActionAsync(
        Document document,
        Diagnostic diagnostic,
        MethodDeclarationSyntax mds,
        CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return null;

        if (!diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetType), out var targetTypeName) ||
            targetTypeName is null ||
            sm.Compilation.GetTypeByMetadataName(targetTypeName) is not { } targetType)
            return null;

        if (!diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetMethod), out var targetMethodName) ||
            targetMethodName is null)
            return null;

        if (!diagnostic.Properties.TryGetValue("ParameterTypes", out var parameterTypeNamesString) ||
            parameterTypeNamesString?.Split([','], StringSplitOptions.RemoveEmptyEntries) is not { } parameterTypeNames)
            return null;

        var parameterTypes =
            parameterTypeNames
                .Choose(n => Optional.MaybeValue(sm.Compilation.GetTypeByMetadataName(n)))
                .ToImmutableArray();

        if (parameterTypes.Length != parameterTypeNames.Length)
            return null;

        var method = targetType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MetadataName == targetMethodName &&
                parameterTypes.Length == m.Parameters.Length &&
                m.Parameters
                    .Select(p => p.Type)
                    .Indexed()
                    .All(p => parameterTypes[p.index].Equals(p.element, SymbolEqualityComparer.Default)))
            .TrySingle();
        
        if (ct.IsCancellationRequested)
            return null;

        return
            method.HasValue ?
                CodeAction.Create(
#if DEBUG
                    $"Change method signature to match target method: " +
                    $"{method.Value.ReturnType} {method.Value.Name}({
                        string.Join(", ", method.Value.Parameters.Select(p => p.Type))})",
#else
                    "Fix method signature",
#endif
                    ct => FixMethodSignatureAsync(document, diagnostic, mds, method.Value, ct)) :
                null;
    }

    private static async Task<Document> FixMethodSignatureAsync(
        Document document,
        Diagnostic diagnostic,
        MethodDeclarationSyntax mds,
        IMethodSymbol targetMethod,
        CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return document;

        var position = mds.GetLocation().SourceSpan.Start;

        IEnumerable<ParameterSyntax> parameters()
        {
            if (!targetMethod.IsStatic)
            {
                yield return Parameter(
                    [],
                    [],
                    IdentifierName(targetMethod.ContainingType.ToMinimalDisplayString(sm, position)),
                    Identifier(default, "instance", default),
                    default
                );
            }

            foreach (var p in targetMethod.Parameters)
            {
                if (ct.IsCancellationRequested)
                    yield break;

                yield return Parameter(
                    [],
                    p.RefKind switch
                    {
                        RefKind.Ref => [Token(SyntaxKind.RefKeyword)],
                        RefKind.Out => [Token(SyntaxKind.OutKeyword)],
                        _ => []
                    },
                    IdentifierName(p.Type.ToMinimalDisplayString(sm, position)),
                    Identifier(default, p.Name, default),
                    default
                );
            }
        }

        var newMds = mds
            .WithReturnType(IdentifierName(targetMethod.ReturnType.ToMinimalDisplayString(sm, position)))
            .WithParameterList(ParameterList(SeparatedList(parameters())));

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(mds, newMds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
