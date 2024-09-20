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

        if (!mds.ParameterList.FullSpan.Contains(context.Position))
            return;

        if (mds.ParameterList.Parameters.FirstOrDefault(p => p.FullSpan.Contains(context.Position - 1)) is { } parameter)
        {
            context.CompletionListSpan = parameter.Span;

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

    private static IEnumerable<CompletionItem> GetCompletions(
        PatchMethodData methodData,
        ITypeSymbol? parameterType,
        SemanticModel sm,
        int position,
        CancellationToken ct)
    {
        if (methodData.TargetMethod is not { } targetMethod)
            yield break;

        var targetType = targetMethod.ContainingType;

        string typeNameString(ITypeSymbol type) => type.ToMinimalDisplayString(sm, position);
        //IdentifierNameSyntax typeNameSyntax(ITypeSymbol type) => IdentifierName(typeNameString(type));

        //SyntaxTokenList Ref() => TokenList(Token(SyntaxKind.RefKeyword));
        //SyntaxTokenList Out() => TokenList(Token(SyntaxKind.OutKeyword));

        if (methodData.PatchType is HarmonyConstants.HarmonyPatchType.Transpiler or HarmonyConstants.HarmonyPatchType.ReversePatch)
            yield break;

        if (ct.IsCancellationRequested)
            yield break;

        IEnumerable<(string parameterName, ITypeSymbol type)> parameters = [];

        if (targetMethod.ContainingType.CanBeReferencedByName)
            parameters = parameters
                .Append((HarmonyConstants.Parameter_injection__instance, targetType));
        else
            parameters = parameters
                .Concat(methodData.TargetMethodInstanceTypes.Select(t => (HarmonyConstants.Parameter_injection__instance, (ITypeSymbol)t)));

        parameters = parameters
            .Append((HarmonyConstants.Parameter_injection__state,
                methodData.Compilation.GetSpecialType(SpecialType.System_Object)))
            .Append((HarmonyConstants.Parameter_injection__runOriginal,
                methodData.Compilation.GetSpecialType(SpecialType.System_Boolean)));

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
                .Where(f => f.CanBeReferencedByName)
                .Select(f => ($"___{f.Name}", f.Type)));

        foreach (var (name, type) in parameters
            .Where(tuple => parameterType is null ||
                sm.Compilation.ClassifyConversion(tuple.type, parameterType).IsStandardImplicit())
            .Select(tuple => (tuple.parameterName, typeNameString(tuple.type))))
        {
            if (ct.IsCancellationRequested)
                yield break;

            var properties = ImmutableDictionary<string, string>.Empty
                .Add("type", type)
                .Add("name", name);
            
            yield return CompletionItem.Create(name,
                filterText: name,
                sortText: name,
                properties: properties,
                tags: [WellKnownTags.Parameter],
                displayTextPrefix: $"{type} ",
                isComplexTextEdit: true);
        }
    }

    private static ParameterSyntax CreateParameter(string type, string name, string? modifier = null) =>
        Parameter(Identifier(name))
            .WithType(IdentifierName(type).WithTrailingTrivia(Whitespace(" ")))
            .WithModifiers(modifier is "ref" ?
                [Token(SyntaxKind.OutKeyword).WithTrailingTrivia(Whitespace(" "))] :
                (modifier is "out" ? [Token(SyntaxKind.OutKeyword).WithTrailingTrivia(Whitespace(" "))] : default));

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        async Task<CompletionChange> noChanges()
        {
            var changes = (await document.GetTextChangesAsync(document, cancellationToken)).ToImmutableArray();

            return CompletionChange.Create(changes.FirstOrDefault(), changes);
        }

        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } syntaxRoot)
        {
            return await noChanges();
        }

        if (!item.Properties.TryGetValue("type", out var type) ||
            !item.Properties.TryGetValue("name", out var name))
        {
#if DEBUG
            throw new InvalidOperationException($"Values missing from properties");
#else
            return await noChanges();
#endif
        }

        var span = item.Span;

        var node = (syntaxRoot.FindNode(span) ?? syntaxRoot.FindToken(span.Start).Parent);
        var pList = node?.FirstAncestorOrSelf<ParameterListSyntax>();

        if (pList is null)
        {
            var spanText = (await document.GetTextAsync(cancellationToken))?.GetSubText(span).ToString();

            throw new InvalidOperationException($"Could not find node for span {span}: '{spanText ?? span.ToString()}'. " +
                $"Token: '{syntaxRoot.FindToken(span.Start, true)}'. Node: '{syntaxRoot.FindNode(span, true, true) ??
                syntaxRoot.FindToken(span.Start).Parent}'. Parameters: {string.Join(", ", pList?.Parameters.Select(p => $"[{p}]"))}.");
        }    

        var parameterNode = node?.FirstAncestorOrSelf<ParameterSyntax>() ??
            pList.Parameters.FirstOrDefault(p => p.Span.Contains(span));

        var newParameter = CreateParameter(type, name);

        if (parameterNode is null)
        {
            // Can't find a parameter to replace when there is no type, so just provide a standard completion
            return CompletionChange.Create(new TextChange(item.Span, newParameter.ToString()));
        }

        if (pList.Parameters.Count != 0 && pList.Parameters.IndexOf(parameterNode) < pList.Parameters.Count - 1)
        {
            newParameter = newParameter.WithTrailingTrivia(Whitespace(" "));
        }

        var newPList = pList.ReplaceNode(parameterNode, newParameter);

        var newDoc = document.WithSyntaxRoot(syntaxRoot.ReplaceNode(pList, newPList));

        var changes = (await newDoc.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false)).ToImmutableArray();

        return CompletionChange.Create(changes.FirstOrDefault(), changes);
    }
}
