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