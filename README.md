# HarmonyAnalyzers

A small set of analyzers (and an even smaller set of code fixes) to assist writing [Harmony](https://harmony.pardeike.net/)
patches, primarily to identify common errors such as missing attributes, typos in names, etc. Also provides
autocomplete suggestions for patch method argument injections.

## Completions

Completion suggestions are provided for patch method parameter injections and for the `argumentTypes` argument to the `[HarmonyPatch]` attribute.

## Rules (as of v1.3)

#### MHA001: Missing class attribute

Methods with `[HarmonyPatch]` attributes in class without a `[HarmonyPatch]` attribute.

#### MHA002: Missing patch type attribute

A method with a `[HarmonyPatch]` patch type method attribute that is not named `Postfix`, `Prefix`, `Transpiler`, etc.

#### MHA003: Missing `MethodType` argument

A `[HarmonyPatch]` attribute without a `methodType` parameter and targeting a method that is not a standard method (eg. property accessor, constructor, indexer).

#### MHA004: Ambiguous match

A patch targeting an overloaded method and without an `argumentTypes` argument.

#### MHA005: No matching method found

No method matching the provided `{HarmonyPatch}` arguments was found.

#### MHA006: No patch methods in patch class

A class with a `[HarmonyPatch]` attribute and no patch methods.

#### MHA007: TargetMethod(s) + HarmonyPatch attribute arguments

A patch class containing `[HarmonyPatch]` arguments and `TargetMethod` or `TargetMethods` methods.

#### MHA008: Assignment to non-ref patch method argument

Assignment to a non-`ref`, non-`out` patch method argument.

#### MHA009: Invalid patch method return type

Patch method return type is not value for its patch type (`IEnumerable<CodeInstruction>` for a transpiler, `void` or `bool` for a prefix, etc.)

#### MHA010: `[HarmonyPatch]` attribute conflict

Multiple `[HarmonyPatch]` attributes applied to a method (and/or class) with conflicting target method arguments.

### MHA011: Multiple patch type attributes on the same method

eg. `HarmonyPostfix` and `HarmonyPrefix`.

#### MHA012: `__result` injection in passthrough Postfix

An injected `__result` argument in a passthrough postfix method's parameters.

#### MHA013: No matching parameter on target method

An injected argument does not match any parameter on the target method.

#### MHA014: Invalid injected parameter type

The type of an injected argument does not match the type of the argument on the target method with the same name.

#### MHA015: Injections in transpiler patch method

Invalid injected arguments in a transpiler method.

#### MHA016: Use `out` for `__state` in prefix patches

An `__state` parameter injection in a prefix patch should be `out`.

#### MHA017: Use of indexed argument injection over parameter name

Argument injections should use the parameter names from the target method and not index (`__0`, `__1`, etc.)

#### MHA018: Reverse patch method signature does not match target method

Non-transpiler reverse patch signature must match the target method.

## Code fixes

Fixes are provided for the following rules:

- [MHA001](#mha001-missing-class-attribute)
- [MHA002](#mha002-missing-patch-type-attribute)
- [MHA003](#mha003-missing-methodtype-argument)
- [MHA008](#mha008-assignment-to-non-ref-patch-method-argument)
- [MHA012](#mha012-__result-injection-in-passthrough-postfix)
- [MHA016](#mha016-use-out-for-__state-in-prefix-patches)
- [MHA017](#mha017-use-of-indexed-argument-injection-over-parameter-name)
- [MHA018](#mha018-reverse-patch-method-signature-does-not-match-target-method)

### Notes

Analysis is currently not fully implemented for reverse patches and not at all for `ReturnRef<T>`.
