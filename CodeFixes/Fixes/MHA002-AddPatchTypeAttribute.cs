using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class AddPatchTypeAttributeCodeFix : PatchClassCodeFixProvider<AddPatchTypeAttributeCodeFix.Descriptor>
{
    public readonly struct Descriptor : IPatchClassCodeFixDescriptor
    {
        public DiagnosticId Id => DiagnosticId.MHA002;
        public string GetTitle(params object[] formatArgs) => string.Format("Add {0} attribute", formatArgs);
        public string GetEquivalenceKey(params object[] formatArgs) => this.GetTitle(formatArgs);
    }

    public override async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<MethodDeclarationSyntax>(location, ct).ConfigureAwait(false) is not { } mds)
            yield break;

        if (sm.GetDeclaredSymbol(mds) is not IMethodSymbol methodSymbol)
            yield break;

        foreach (var t in GetValidAttributeTypes(diagnostic, sm, methodSymbol, ct)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
        {
            if (ct.IsCancellationRequested)
                yield break;

            var title = /*$"Add {t.Name} attribute";*/
                GetTitle(t.Name);

            yield return CodeAction.Create(
                title, ct => AddAttributeActionAsync(document, mds, sm, t, ct), equivalenceKey: title);
        }
    }

    private static async Task<Document> AddAttributeActionAsync(
        Document document,
        MethodDeclarationSyntax mds,
        SemanticModel sm,
        INamedTypeSymbol t,
        CancellationToken ct)
    {
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

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(mds, newMds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }

    private static IEnumerable<INamedTypeSymbol> GetValidAttributeTypes(
        Diagnostic diagnostic,
        SemanticModel sm,
        IMethodSymbol symbol,
        CancellationToken ct)
    {
        var patchTypeAttributes = HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(sm.Compilation, ct);
        
        IEnumerable<IMethodSymbol> targetMethodCandidates = [];

        if (!diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetMethod), out var targetMethodName) || 
            targetMethodName is null)
            _ = diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetMethodName), out targetMethodName);

        if (diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetType), out var targetTypeName) && 
            targetTypeName is not null && 
            targetMethodName is not null)
        {
            targetMethodCandidates = sm.Compilation.GetTypeByMetadataName(targetTypeName)?.GetMembers()
                .OfType<IMethodSymbol>().Where(m => m.MetadataName == targetMethodName) ?? [];
        }

        foreach (var ((patchType, attributeType), targetMethod) in
            patchTypeAttributes.CartesianProduct(targetMethodCandidates.DefaultIfEmpty()))
        {
            var validReturnTypes = HarmonyHelpers.ValidReturnTypes(
                patchType,
                sm.Compilation,
                ct,
                targetMethod?.ReturnType,
                symbol.MayBePassthroughPostfix(targetMethod, sm.Compilation));

            if (validReturnTypes.Any(validReturnType => sm.Compilation.ClassifyConversion(symbol.ReturnType, validReturnType).IsStandardImplicit()))
            {
                yield return attributeType;
            }
        }
    }
}
