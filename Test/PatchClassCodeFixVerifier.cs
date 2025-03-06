using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing;

using MicroUtils.HarmonyAnalyzers.CodeFixes;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassCodeFixVerifier<TCodeFix> : CodeFixVerifier<
    PatchClassAnalyzer,
    TCodeFix,
    PatchClassCodeFixTest<TCodeFix>,
    DefaultVerifier>
    where TCodeFix : CodeFixProvider, new()
{
    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string codeActionKey, string fixedSource)
        => VerifyCodeFixAsync(source, [expected], codeActionKey, fixedSource);
    
    public static Task VerifyCodeFixAsync(string source, IEnumerable<DiagnosticResult> expected, string codeActionKey, string fixedSource)
    {
        var test = new PatchClassCodeFixTest<TCodeFix>
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = codeActionKey
        };
        
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(CancellationToken.None);
    }
}
