using System.Diagnostics;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassCodeFixTest<TCodeFix> : CSharpCodeFixTest<PatchClassAnalyzer, TCodeFix, DefaultVerifier>
    where TCodeFix : CodeFixProvider, new()
{
    public PatchClassCodeFixTest()
    {
        this.ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages([HarmonyPackage]);
        this.DisabledDiagnostics.AddRange(Default.DisabledDiagnostics);
    }

    const string targetClassSourceName = "TargetClass.cs";

    public string? TargetClassCode
    {
        get;
        set
        {
            _ = this.TestState.Sources.RemoveAll(s => s.filename == targetClassSourceName);
            _ = this.FixedState.Sources.RemoveAll(s => s.filename == targetClassSourceName);

            if (value is not null)
            {
                this.TestState.Sources.Add((targetClassSourceName, value));
                this.FixedState.Sources.Add((targetClassSourceName, value));
            }

            field = value;

        }
    }

    const string patchCodeSourceName = "Patch.cs";

    public string? TestPatchCode
    {
        get;
        set
        {
            _ = this.TestState.Sources.RemoveAll(s => s.filename == patchCodeSourceName);

            if (value is not null)
                this.TestState.Sources.Add((patchCodeSourceName, value));

            field = value;

        }
    }

    public string? FixedPatchCode
    {
        get;
        set
        {
            _ = this.FixedState.Sources.RemoveAll(s => s.filename == patchCodeSourceName);

            if (value is not null)
                this.FixedState.Sources.Add((patchCodeSourceName, value));

            field = value;

        }
    }

    public void AddSources(TestSources sources)
    {
        this.TargetClassCode = sources.TargetClassSource;
        this.TestPatchCode = sources.TestPatchSource;
        this.FixedPatchCode = sources.FixedPatchSource;
    }

    public Func<CodeAction, bool>? ActionFilter { get; set; }

    private static string PrintActions(IEnumerable<CodeAction> actions)
    {
        var sb = new StringBuilder();
            //.Append("Available actions:");

        foreach (var action in actions)
        {
            sb = sb.AppendLine($" - {action.EquivalenceKey}");
        }

        return sb.ToString();
    }

    protected override ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
    {
        actions = base.FilterCodeActions(actions);

        if (this.CodeActionEquivalenceKey is null)
        {
            return actions;
        }

        var matchingActions = actions
            .Where(action => action.EquivalenceKey == this.CodeActionEquivalenceKey && (this.ActionFilter?.Invoke(action) ?? true))
            .ToImmutableArray();

        if (!matchingActions.Any())
        {
            Debug.Print($"No matching code actions for key '{this.CodeActionEquivalenceKey}'");
        }

        Debug.Print("Available actions:");
        Debug.Print(PrintActions(actions));

        Debug.Print("Selected actions:");
        Debug.Print(PrintActions(matchingActions));

        return matchingActions;
    }
}
