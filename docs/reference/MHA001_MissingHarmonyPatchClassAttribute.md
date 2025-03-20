# MHA001: Missing `[HarmonyPatch]` class attribute

A class containing methods with Harmony attributes must have a `HarmonyPatch` attribute on the class declaration.

## Example Violation

```cs
static class PatchClass
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    [HarmonyPostfix]
    static void PatchMethod()
    {
        
    }
}
```

### Fix

```cs
[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    [HarmonyPostfix]
    static void PatchMethod()
    {
        
    }
}
```
