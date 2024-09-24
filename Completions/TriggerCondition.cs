using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace MicroUtils.HarmonyAnalyzers.Completions;
internal static class TriggerCondition
{
    public static bool ShouldTrigger(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options, bool isInjection = false)
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
            (CompletionTriggerKind.Insertion, _) => isInjection && injectionPrefix(),
            _ => false
        };

        return shouldTrigger;
    }
}
