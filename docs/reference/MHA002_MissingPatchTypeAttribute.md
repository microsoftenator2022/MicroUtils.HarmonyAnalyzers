# MHA002: Missing Patch Type Attribute

A patch method must have a suitable patch type attribute or its name must exactly match a patch type.

## Examples

### Method with harmony pactch type attributes
Harmony patch type attributes include:
- `HarmonyPrefix`
- `HarmonyPostfix`
- `HarmonyTranspiler`
- `HarmonyReversePatch`
- etc.

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{

    [HarmonyPrefix]
    static void PrefixPatch()
    {
        
    }
}
```

### Method name that match a Harmony patch type

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Postfix()
    {

    }
}
```

### Example violation

Method name is not a Harmony patch type and does not have a patch type attribute

```cs
[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void PatchMethod()
    {

    }
}
```

### Fix

Add an appropriate patch type attribute

```cs
[HarmonyPatch]
static class PatchClass
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void PatchMethod()
    {

    }
}
```