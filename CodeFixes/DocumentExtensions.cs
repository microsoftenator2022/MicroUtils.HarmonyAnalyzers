using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;

static class DocumentExtensions
{
    public static async Task<SemanticModel?> GetIgnoreAccessSemanticModelAsync(this Document document, CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return null;

        if (await document.GetSyntaxTreeAsync(ct) is not { } st)
            return null;

        var compilation = sm.Compilation.WithAllMembers();
        return compilation.GetSemanticModel(st, true);
    }

    public static async Task<TNode?> FindSyntaxNodeAsync<TNode>(this Document document, Location location, CancellationToken ct)
        where TNode : SyntaxNode
    {
        if (await document.GetSyntaxRootAsync(ct) is not { } syntaxRoot)
            return null;

        return syntaxRoot.FindNode(location.SourceSpan) as TNode;
    }
}
