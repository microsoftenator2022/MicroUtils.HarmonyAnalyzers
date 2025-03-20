# MHA018: Reverse patch signature does not match target method

The signature of a (non-transpiler) reverse patch must match the signature of the target method.

## Example Violation

```cs
class TargetClass
{
    object TargetMethod(string s)
    {
        //...
    }
}

[HarmonyPatch]
static class Patch
{

    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    [HarmonyReversePatch]
    static void ReversePatch() => throw new NotImplementedException();
}
```

### Fix

```cs
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
[HarmonyReversePatch]
static object ReversePatch(TargetClass instance, string s) => throw new NotImplementedException();
```
