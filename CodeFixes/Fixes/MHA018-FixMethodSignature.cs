using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA018;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class FixMethodSignatureCodeFix : PatchClassCodeFixProvider<FixMethodSignature> { }

public readonly struct FixMethodSignature : IHarmonyCodeFix
{
    public DiagnosticId DiagnosticId => DiagnosticId.MHA018;

    public string GetTitle(params object[] formatArgs) =>
#if DEBUG
        string.Format("Change method signature to match target method: {0} {1}({2})", formatArgs);
#else
            "Fix method signature";
#endif

    public string GetEquivalenceKey(params object[] formatArgs) => this.GetTitle(formatArgs);

    async IAsyncEnumerable<CodeAction> IHarmonyCodeFix.GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (!diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetType), out var targetTypeName) ||
            targetTypeName is null ||
            sm.Compilation.GetTypeByMetadataName(targetTypeName) is not { } targetType)
            yield break;

        if (!diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetMethod), out var targetMethodName) ||
            targetMethodName is null)
            yield break;

        if (!diagnostic.Properties.TryGetValue("ParameterTypes", out var parameterTypeNamesString) ||
            parameterTypeNamesString?.Split([','], StringSplitOptions.RemoveEmptyEntries) is not { } parameterTypeNames)
            yield break;

        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<MethodDeclarationSyntax>(location, ct).ConfigureAwait(false) is not { } mds)
            yield break;

        var parameterTypes =
            parameterTypeNames
                .Choose(n => Optional.MaybeValue(sm.Compilation.GetTypeByMetadataName(n)))
                .ToImmutableArray();

        if (parameterTypes.Length != parameterTypeNames.Length)
            yield break;

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
            yield break;

        if (!method.HasValue)
            yield break;

        yield return CodeAction.Create(
//#if DEBUG
//            $"Change method signature to match target method: " +
//            $"{method.Value.ReturnType} {method.Value.Name}({
//                string.Join(", ", method.Value.Parameters.Select(p => p.Type))})",
//#else
//            "Fix method signature",
//#endif
            GetTitle(method.Value.ReturnType, method.Value.Name, string.Join(", ", method.Value.Parameters.Select(p => p.Type))),

            ct => FixMethodSignatureAsync(document, diagnostic, sm, mds, method.Value, ct));
    }

    private static async Task<Document> FixMethodSignatureAsync(
        Document document,
        Diagnostic diagnostic,
        SemanticModel sm,
        MethodDeclarationSyntax mds,
        IMethodSymbol targetMethod,
        CancellationToken ct)
    {
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

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(mds, newMds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
