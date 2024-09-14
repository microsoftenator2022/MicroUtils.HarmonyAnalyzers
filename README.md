# HarmonyAnalyzers

A small set of analyzers (and an even smaller set of code fixes) to assist writing [Harmony](https://harmony.pardeike.net/) patches, 
primarily to identify common errors such as missing attributes, typos in names, etc.

Analysis is currently not implemented for patches with method type = `MethodType.Async` or `MethodType.Enumerator`, and `ReturnRef<T>` parameters.