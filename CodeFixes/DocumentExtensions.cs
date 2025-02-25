using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;

static class DocumentExtensions
{

    // FIXME: Should cache the new compilation somewhere within the CodeFixContext/CompletionContext
    // Also maybe cache the Semantic model (per-Document?)
    public static async Task<SemanticModel?> GetIgnoreAccessSemanticModelAsync(this Document document, CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return null;

        if (await document.GetSyntaxTreeAsync(ct) is not { } st)
            return null;

        var compilation = sm.Compilation.WithAllMembers();
        return compilation.GetSemanticModel(st, true);
    }
}
