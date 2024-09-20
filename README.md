# HarmonyAnalyzers

A small set of analyzers (and an even smaller set of code fixes) to assist writing [Harmony](https://harmony.pardeike.net/)
patches, primarily to identify common errors such as missing attributes, typos in names, etc. Also provides
autocomplete suggestions for patch method argument injections.

Analysis is currently not fully implemented for reverse patches, patches with method type = `MethodType.Async`,
and `ReturnRef<T>` parameters.