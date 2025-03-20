# MHA007: Multiple target method definitions for patch class

A patch class may not have more than one of:
1. A `HarmonyPatch` attribute with provided target method arguments
2. Target method member (`TargetMethod()` or `[HarmonyTargetMethod]`)
3. Target methods member (`TargetMethods()` or `[HarmonyTargetMethods]`)

## Example Violation

Incorrectly using `TargetMethod` and `HarmonyPatch` arguments

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static MethodInfo TargetMethod() =>
        typeof(TargetType).GetMethod("AnotherTargetMethod");
}
```

### Fix

Use separate patch classes (eg. nested classes) if you wish to mix `TargetMethod`/`TargetMethods` and `HarmonyPatch` target methods.