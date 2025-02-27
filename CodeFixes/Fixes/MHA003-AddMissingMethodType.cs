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

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA003;
using static SyntaxFactory;
internal readonly struct AddMissingMethodType : IHarmonyCodeFix
{
    public DiagnosticId DiagnosticId => DiagnosticId.MHA003;

    public async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<MethodDeclarationSyntax>(location, ct).ConfigureAwait(false) is not { } mds)
            yield break;

        if (!diagnostic.Properties.TryGetValue(nameof(PatchMethodData.TargetMethodType), out var methodTypeName) ||
            !Enum.TryParse<HarmonyConstants.PatchTargetMethodType>(methodTypeName, out var methodType))
            yield break;

        if (HarmonyHelpers.GetHarmonyMethodTypeType(sm.Compilation, ct) is not INamedTypeSymbol methodTypeType)
            yield break;

        var enumField = methodTypeType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => f.HasConstantValue && (f.ConstantValue as int?) == (int)methodType);

        var title = $"Add {enumField.ToMinimalDisplayString(sm, mds.SpanStart)} with HarmonyPatch attribute";

        yield return CodeAction.Create(
            title,
            ct => AddMethodTypeAttributeAsync(document, mds, sm, methodType, methodTypeType, enumField, ct),
            equivalenceKey: title);
    }

    private static async Task<Document> AddMethodTypeAttributeAsync(
        Document document,
        MethodDeclarationSyntax mds,
        SemanticModel sm,
        HarmonyConstants.PatchTargetMethodType methodType,
        INamedTypeSymbol methodTypeType,
        IFieldSymbol enumField,
        CancellationToken ct)
    {
        if (sm.Compilation.GetType(
            HarmonyConstants.Namespace_HarmonyLib,
            HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch, ct) is not { } patchAttributeType)
            return document;

        var newMds = mds.AddAttributeLists(
            AttributeList(
                SeparatedList(
                [
                    Attribute(
                        IdentifierName(patchAttributeType.ToMinimalDisplayString(sm, mds.SpanStart)),
                        AttributeArgumentList(
                        [
                            AttributeArgument(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(methodTypeType.ToMinimalDisplayString(sm, mds.SpanStart)),
                                    IdentifierName(enumField.Name)

                                )
                            )
                        ])
                    )
                ])
            )
        );

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(mds, newMds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}

