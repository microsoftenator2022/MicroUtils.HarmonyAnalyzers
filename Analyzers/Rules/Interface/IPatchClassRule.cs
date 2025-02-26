using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal interface IPatchClassRule : IPatchRule
{
    ImmutableArray<Diagnostic> Check(
        PatchClassData patchClassData,
        CancellationToken cancellationToken);
}

internal static partial class PatchRule
{
    public static ImmutableArray<Diagnostic> Check<TRule>(
        PatchClassData patchClassData,
        CancellationToken cancellationToken) where TRule : struct, IPatchClassRule
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        return default(TRule).Check(
            patchClassData,
            cancellationToken);
    }
}
