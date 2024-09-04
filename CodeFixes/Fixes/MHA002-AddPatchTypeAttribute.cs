using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

using static SyntaxFactory;

internal class AddPatchTypeAttribute
{
    internal static async Task<ImmutableArray<CodeAction>> GetActions(CodeFixContext context, Diagnostic diagnostic, MethodDeclarationSyntax mds)
    {
        var document = context.Document;
        var ct = context.CancellationToken;

        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return [];

        if (sm.GetDeclaredSymbol(mds) is not IMethodSymbol methodSymbol)
            return [];

        return GetValidAttributeTypes(sm, diagnostic, methodSymbol, ct)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .Select(t =>
            {
                var title = $"Add {t.Name} attribute";
                return GetAction(title, document, mds, t);
            })
            .ToImmutableArray();
    }

    internal static CodeAction GetAction(string title, Document document, MethodDeclarationSyntax mds, INamedTypeSymbol t) =>
        CodeAction.Create(title, ct => AddAttributeAction(document, mds, t, ct), equivalenceKey: title);

    private static async Task<Document> AddAttributeAction(Document document, MethodDeclarationSyntax mds, INamedTypeSymbol t, CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return document;

        var newMds = mds.AddAttributeLists(
            AttributeList(
                SeparatedList(
                [
                    Attribute(
                        IdentifierName(t.ToMinimalDisplayString(sm, mds.SpanStart))
                    )
                ])
            )
        );

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(mds, newMds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }

    private static IEnumerable<INamedTypeSymbol> GetValidAttributeTypes(SemanticModel sm, Diagnostic diagnostic, IMethodSymbol symbol, CancellationToken ct)
    {
        var patchTypeAttributes = HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(sm.Compilation, ct);
        
        IEnumerable<IMethodSymbol> targetMethodCandidates = [];

        if (diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetType), out var targetTypeName) && targetTypeName is not null &&
            diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetMethod), out var targetMethodName) && targetMethodName is not null)
        {
            targetMethodCandidates = sm.Compilation.GetTypeByMetadataName(targetTypeName)?.GetMembers()
                .OfType<IMethodSymbol>().Where(m => m.MetadataName == targetMethodName) ?? [];
        }

        foreach (var ((patchType, attributeType), targetMethod) in patchTypeAttributes
            .Join(targetMethodCandidates.DefaultIfEmpty(), _ => true, _ => true, (a, b) => (a, b)))
        {
            if (HarmonyHelpers.ValidReturnTypes(patchType, sm.Compilation, ct, targetMethod, symbol.ReturnTypeMatchesFirstParameter())
                .Any(validReturnType => sm.Compilation.ClassifyConversion(symbol.ReturnType, validReturnType).IsImplicit))
            {
                yield return attributeType;
            }
        }
    }
}
