using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace MicroUtils.HarmonyAnalyzers.Completions;

using static SyntaxFactory;

[ExportCompletionProvider(nameof(PatchMethodParametersProvider), LanguageNames.CSharp)]
internal class PatchMethodParametersProvider : CompletionProvider
{
    public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
    {
        //bool isNewIdentifier() =>
        //    SyntaxFacts.IsIdentifierStartCharacter(text[caretPosition -1]) &&
        //    !SyntaxFacts.IsIdentifierPartCharacter(text[caretPosition - 2]) &&
        //    !SyntaxFacts.IsIdentifierPartCharacter(text[caretPosition]);

        bool injectionPrefix()
        {
            if (text[caretPosition - 1] is not '_' || text[caretPosition - 2] is not '_')
                return false;

            var i = 3;
            if (text[i] is '_')
            {
                i++;
            }

            return !SyntaxFacts.IsIdentifierPartCharacter(text[i]);
        }

        var shouldTrigger = (trigger.Kind, trigger.Character) switch
        {
            (CompletionTriggerKind.Invoke, _) => true,
            (CompletionTriggerKind.InvokeAndCommitIfUnique, _) => true,
            (CompletionTriggerKind.Insertion, ' ') => true,
            (CompletionTriggerKind.Insertion, _) => injectionPrefix(),
            _ => false
        };

        return shouldTrigger;
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        //if (context.Document.Project.Services.GetService<CompletionService>() is not { } service)
        //    return;

        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not { } syntax)
            return;

        if (syntax.FindNode(context.CompletionListSpan)?.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } mds)
            return;

        if (!mds.ParameterList.FullSpan.Contains(context.CompletionListSpan))
            return;

        if (await context.Document.GetSemanticModelAsync(context.CancellationToken) is not { } sm)
            return;

        if (sm.GetDeclaredSymbol(mds) is not IMethodSymbol methodSymbol)
            return;

        var maybeMethodData = PatchMethodData.TryCreate(methodSymbol, sm.Compilation, ct: context.CancellationToken);

        if (!maybeMethodData.HasValue)
            return;

        var methodData = maybeMethodData.Value;

        var text = await context.Document.GetTextAsync(context.CancellationToken);

        ITypeSymbol? parameterType = null;

        TextSpan span = context.CompletionListSpan;

        if (!mds.ParameterList.FullSpan.Contains(context.Position))
            return;

        var token = mds.FindToken(context.Position - 1);

        if (token.Parent?.FirstAncestorOrSelf<ParameterSyntax>() is { } parameter)
        {
            context.CompletionListSpan = new TextSpan(parameter.Identifier.Span.Start, Math.Max(0, context.Position - parameter.Identifier.Span.Start));

            if (parameter.Type is { } parameterTypeSyntax)
            {
                parameterType = sm.GetTypeInfo(parameterTypeSyntax).Type;
                context.IsExclusive = true;
            }

            if (parameterType?.TypeKind is TypeKind.Error or TypeKind.Unknown)
                parameterType = null;
        }

        context.AddItems(GetCompletions(methodData, parameterType, sm, context.Position, context.CancellationToken)
            .Where(c => !mds.ParameterList.Parameters.Any(p => p.Identifier.ValueText == c.Properties["name"])));
    }

    //private static ParameterSyntax CreateParameter(string type, string name, string? modifier = null) =>
    //    Parameter(Identifier(name))
    //        .WithType(IdentifierName(type).WithTrailingTrivia(Whitespace(" ")))
    //        .WithModifiers(modifier is "ref" ?
    //            [Token(SyntaxKind.OutKeyword).WithTrailingTrivia(Whitespace(" "))] :
    //            (modifier is "out" ? [Token(SyntaxKind.OutKeyword).WithTrailingTrivia(Whitespace(" "))] : default));

    private static IEnumerable<CompletionItem> GetCompletions(
        PatchMethodData methodData,
        ITypeSymbol? parameterType,
        SemanticModel sm,
        int position,
        CancellationToken ct)
    {
        if (methodData.TargetType is not { } targetType ||
            methodData.TargetMethod is not { } targetMethod)
            yield break;

        string typeNameString(ITypeSymbol type) => type.ToMinimalDisplayString(sm, position);
        //IdentifierNameSyntax typeNameSyntax(ITypeSymbol type) => IdentifierName(typeNameString(type));

        //SyntaxTokenList Ref() => TokenList(Token(SyntaxKind.RefKeyword));
        //SyntaxTokenList Out() => TokenList(Token(SyntaxKind.OutKeyword));

        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Transpiler or HarmonyConstants.HarmonyPatchType.ReversePatch)
            yield break;

        if (ct.IsCancellationRequested)
            yield break;

        IEnumerable<(string parameterName, ITypeSymbol type)> parameters = [];

        parameters = parameters
            .Append((HarmonyConstants.Parameter_injection__instance, targetType));

        if (typeof(object).ToNamedTypeSymbol(sm.Compilation) is ITypeSymbol objectType)
        {
            parameters = parameters
                .Append((HarmonyConstants.Parameter_injection__state, objectType));
        }

        if (typeof(bool).ToNamedTypeSymbol(sm.Compilation) is ITypeSymbol boolType)
        {
            parameters = parameters
                .Append((HarmonyConstants.Parameter_injection__runOriginal, boolType));
        }

        if (!targetMethod.ReturnsVoid)
        {
            parameters = parameters
                .Append((HarmonyConstants.Parameter_injection__result, targetMethod.ReturnType));
        }

        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Finalizer &&
            typeof(Exception).ToNamedTypeSymbol(sm.Compilation) is { } exceptionType)
        {
            parameters = parameters
                .Append((HarmonyConstants.Parameter_injection__exception, exceptionType));
        }

        parameters = parameters
            .Concat(targetMethod.Parameters.Select(p => (p.Name, p.Type)));

        parameters = parameters
            .Concat(targetType.GetMembers()
                .OfType<IFieldSymbol>()
                .Select(f => ($"___{f.Name}", f.Type)));

        foreach (var (name, type) in parameters
            .Where(tuple => parameterType is null ||
                sm.Compilation.ClassifyConversion(tuple.type, parameterType).IsStandardImplicit())
            .Select(tuple => (tuple.parameterName, typeNameString(tuple.type))))
        {
            if (ct.IsCancellationRequested)
                yield break;

            var properties = ImmutableDictionary<string, string>.Empty
                .Add("name", name);

            if (parameterType is null)
                properties = properties.Add("type", type);
            
            yield return CompletionItem.Create(name,
                filterText: name,
                sortText: name,
                properties: properties,
                tags: [WellKnownTags.Parameter],
                displayTextPrefix: $"{type} ",
                isComplexTextEdit: true);
        }
    }

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        var value = item.Properties["name"];

        if (item.Properties.TryGetValue("type", out var type))
        {
            value = $"{type} {value}";
        }

        return await Task.FromResult(CompletionChange.Create(textChange: new TextChange(item.Span, value)));
    }
}
