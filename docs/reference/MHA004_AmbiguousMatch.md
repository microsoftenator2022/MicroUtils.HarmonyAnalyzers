# MHA004: Ambiguous Match

When a target method is overloaded, provide an `argumentTypes` argument to a `HarmonyPatch` attribute.

## Example Violation

```cs
class TargetType
{
    void TargetMethod() {}

    string TargetMethod(int i) => "";
}

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Postfix()
    {

    }
}
```

### Fix

Provide argument types array to a `HarmonyPatch` attribute

```cs

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod), new Type[] { typeof(int) })]
static class PatchClass
{
    static void Postfix()
    {

    }
}
```