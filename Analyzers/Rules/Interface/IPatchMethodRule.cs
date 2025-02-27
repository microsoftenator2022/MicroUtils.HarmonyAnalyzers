using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

internal interface IPatchMethodRule : IPatchRule
{
    ImmutableArray<Diagnostic> Check(
        PatchMethodData patchMethodData,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);
}

internal static partial class PatchRule
{
    public static ImmutableArray<Diagnostic> Check<TRule>(
        PatchMethodData patchMethodData,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) where TRule : struct, IPatchMethodRule
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        return default(TRule).Check(
            patchMethodData,
            semanticModel,
            cancellationToken);
    }
}
