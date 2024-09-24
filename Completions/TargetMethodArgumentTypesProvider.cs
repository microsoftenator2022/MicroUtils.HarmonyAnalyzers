using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace MicroUtils.HarmonyAnalyzers.Completions;

using static SyntaxFactory;

[ExportCompletionProvider(nameof(PatchMethodParametersProvider), LanguageNames.CSharp)]
internal class TargetMethodArgumentTypesProvider : CompletionProvider
{
    public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options) =>
        TriggerCondition.ShouldTrigger(text, caretPosition, trigger, options);

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not { } syntax ||
            await context.Document.GetSemanticModelAsync(context.CancellationToken) is not { } sm
            //||
            //typeof(Type).ToNamedTypeSymbol(sm.Compilation)?.ToMinimalDisplayString(sm, context.Position) is not { } typeTypeName
            )
            return;

        if (syntax.FindNode(context.CompletionListSpan) is not { } node ||
            node.FirstAncestorOrSelf<AttributeArgumentListSyntax>() is not { } attributeArgumentList ||
            attributeArgumentList.Parent is not AttributeSyntax attributeSyntax)
            return;

        var symbolInfo = sm.GetSymbolInfo(attributeSyntax, context.CancellationToken);

        if (symbolInfo.Symbol is null &&
            symbolInfo.CandidateSymbols.IsDefaultOrEmpty)
            return;

        if (attributeSyntax.FindAttributeData(sm, context.CancellationToken) is not { } attribute)
            return;
        
        if (attribute.AttributeClass is not INamedTypeSymbol attributeType ||
            !attributeType.Equals(HarmonyHelpers.GetHarmonyPatchType(sm.Compilation, context.CancellationToken), SymbolEqualityComparer.Default))
            return;

        var missingArg = attributeArgumentList.Arguments.Indexed().TryFirst(a =>
        {
            if (!a.element.IsMissing && sm.GetTypeInfo(a.element.Expression).Type is not null)
                return false;

            if (a.element.GetLocation().SourceSpan.End >= context.Position)
                return true;

            return false;
        });

        if (!missingArg.HasValue)
            return;

        var candidateAttributeConstructors = sm.FilterCandidateMethods(symbolInfo, attributeArgumentList, context.CancellationToken)
            .Where(c => c.Constructor.Parameters[missingArg.Value.index].Name is HarmonyConstants.Parameter_argumentTypes)
            .ToImmutableArray();

        if (candidateAttributeConstructors.Length < 1)
            return;

        context.CompletionListSpan = 
            missingArg.Value.element.IsMissing ?
                new(context.Position, missingArg.Value.element.Span.End) :
                missingArg.Value.element.Span;

        var nameColon = false;

        if (context.Trigger.Kind is CompletionTriggerKind.Invoke ||
            missingArg.Value.element.NameColon?.Name.ToString() is HarmonyConstants.Parameter_argumentTypes)
        {
            nameColon = true;
        }

        if (attribute.ApplicationSyntaxReference is null ||
            (await attribute.ApplicationSyntaxReference.GetSyntaxAsync(context.CancellationToken))
                .FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } mds ||
            sm.GetDeclaredSymbol(mds) is not IMethodSymbol methodSymbol)
            return;

        var maybeMethodData = PatchMethodData.TryCreate(methodSymbol, sm.Compilation, ct: context.CancellationToken);

        if (!maybeMethodData.HasValue)
            return;

        var methodData = maybeMethodData.Value;

        foreach (var (m, args) in candidateAttributeConstructors)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            if (methodData.TargetType is not null && methodData.TargetMethodName is not null)
                break;

            foreach (var (p, arg) in args)
            {
                switch (p.Name)
                {
                    case HarmonyConstants.Parameter_declaringType:
                        if (methodData.TargetType is null && arg.Expression is TypeOfExpressionSyntax toes &&
                            sm.GetSymbolInfo(toes.Type).Symbol is INamedTypeSymbol t)
                        {
                            methodData = methodData with { TargetType = t };
                        }
                        break;
                    case HarmonyConstants.Parameter_methodName:
                        if (methodData.TargetMethodName is null &&
                            sm.GetConstantValue(arg.Expression).Value is string n)
                            methodData = methodData with { TargetMethodName = n };

                        break;
                }
            }
        }

        var candidateTargetMethods = methodData.GetCandidateMethods().ToImmutableArray();

        if (candidateTargetMethods.Length < 1)
            return;

        if (nameColon)
            context.IsExclusive = true;

        foreach (var iaces in candidateTargetMethods
            .Select(m => CreateTypeArrayCreationSyntax(m, sm, context.Position, nameColon)))
        {
            var s = iaces.ToFullString();

            var ci = CompletionItem.Create(
                displayText: s,
                tags: [WellKnownTags.Parameter]);

            context.AddItem(ci);
        }
    }

    private static AttributeArgumentSyntax CreateTypeArrayCreationSyntax(
        IMethodSymbol method,
        SemanticModel sm,
        int position,
        bool includeNameColon = false)
    {
        var ps = method.Parameters.Select(p =>
            TypeOfExpression(
                IdentifierName(
                    p.Type.ToMinimalDisplayString(sm, position)
                )
            ));
        
        var implicitArrayExpr = ImplicitArrayCreationExpression(
            InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                Token(SyntaxKind.OpenBraceToken),
                SeparatedList<ExpressionSyntax>(ps.Select(p => p.WithLeadingTrivia(Space))),
                Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(Space)
            ).WithLeadingTrivia(Space)
        );

        var nameColon = NameColon(HarmonyConstants.Parameter_argumentTypes).WithTrailingTrivia(Space);

        //var collectionInitializerExpr =
        //    CollectionExpression(SeparatedList<CollectionElementSyntax>(ps.Select(ExpressionElement))).NormalizeWhitespace();

        return AttributeArgument(
            nameEquals: null,
            nameColon: includeNameColon ? nameColon : null,
            implicitArrayExpr);
    }
}
