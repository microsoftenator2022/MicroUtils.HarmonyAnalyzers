using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using MicroUtils.HarmonyAnalyzers.CodeFixes;

namespace MicroUtils.HarmonyAnalyzers.Test;

using static CommonTestProperties;

internal class PatchClassCodeFixTest<TCodeFix> : CSharpCodeFixTest<PatchClassAnalyzer, TCodeFix, DefaultVerifier>
    where TCodeFix : CodeFixProvider, new()
{
    public PatchClassCodeFixTest()
    {
        this.ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages([HarmonyPackage]);
        this.DisabledDiagnostics.AddRange(IgnoreDiagnostics);
    }

    public Func<CodeAction, bool>? ActionFilter { get; set; }

    private static string PrintActions(IEnumerable<CodeAction> actions)
    {
        var sb = new StringBuilder()
            .Append("Available actions:");

        foreach (var action in actions)
        {
            sb = sb.AppendLine()
                .Append($" - {action.EquivalenceKey}");
        }

        return sb.ToString();
    }

    protected override ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
    {
        actions = base.FilterCodeActions(actions);

        Debug.Print(PrintActions(actions));

        if (this.CodeActionEquivalenceKey is null)
        {
            return actions;
        }

        var matchingActions = actions
            .Where(action => action.EquivalenceKey == this.CodeActionEquivalenceKey && (this.ActionFilter?.Invoke(action) ?? true))
            .ToImmutableArray();

        if (!matchingActions.Any())
        {
            var sb = new StringBuilder()
                .Append($"No matching code actions for key '{this.CodeActionEquivalenceKey}'");
                
            if (actions.Any())
            {
                sb = sb.AppendLine()
                    .Append(PrintActions(actions));
            }

            throw new Exception(sb.ToString());
        }

        return matchingActions;
    }
}
