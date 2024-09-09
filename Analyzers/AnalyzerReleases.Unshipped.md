; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID |     Category     | Severity | Notes
--------|------------------|----------|----------------------------------------------------
MHA016  | PatchMethod      | Info     | Use `out` for `__state` in prefix patches
MHA017  | PatchMethod      | Info     | Using `__0`, `__1`, etc. injections over parameter names
MHA018  | PatchMethod      | Warning  | Reverse patch method signature does not match target method