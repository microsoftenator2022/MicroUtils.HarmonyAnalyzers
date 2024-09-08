; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID |     Category     | Severity | Notes
--------|------------------|----------|----------------------------------------------------
MHA001  | PatchAttribute   | Warning  | Missing class attribute
MHA002  | PatchAttribute   | Warning  | Missing patch type attribute
MHA003  | TargetMethod     | Warning  | Missing MethodType argument
MHA004  | TargetMethod     | Warning  | Ambiguous match
MHA005  | TargetMethod     | Warning  | No matching method found
MHA006  | PatchMethod      | Warning  | No patch methods in patch class
MHA007  | TargetMethod     | Warning  | TargetMethod(s) + HarmonyPatch attribute arguments
MHA008  | PatchMethod      | Warning  | Assignment to non-ref patch method argument
MHA009  | PatchMethod      | Warning  | Invalid patch method return type
MHA010  | PatchAttribute   | Warning  | HarmonyPatch attribute conflict
MHA011  | PatchMethod      | Warning  | Multiple patch type attributes on the same method
MHA012  | PatchMethod      | Info     | `__result` injection in passthrough Postfix
MHA013  | PatchMethod      | Warning  | No matching parameter on target method
MHA014  | PatchMethod      | Warning  | Invalid injected parameter type
MHA015  | PatchMethod      | Warning  | Injections in Transpiler patch method
MHA016  | PatchMethod      | Info     | Use `out` for `__state` in prefix patches
MHA017  | PatchMethod      | Info     | Using `__0`, `__1`, etc. injections over parameter names
MHA018  | PatchMethod      | Warning  | Reverse patch method signature does not match target method