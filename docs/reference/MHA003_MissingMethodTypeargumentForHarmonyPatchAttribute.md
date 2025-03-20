# MHA003: Missing `MethodType` argument to `[HarmonyPatch]` attribute

When targeting a method that is not a "standard" method (eg. constructor, property accessor, etc.) a MethodType argument must be provided to a `HarmonyPatch` attribute.

```cs
class TargetType
{
    public int TargetProperty => 0;
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetProperty), MethodType.Getter)]
    [HarmonyPostfix]
    static void PatchMethod()
    {

    }
}
```
