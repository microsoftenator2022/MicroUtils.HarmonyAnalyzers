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

        return Util.GetIgnoreAccessSemanticModel(sm.Compilation, st);
    }
}
