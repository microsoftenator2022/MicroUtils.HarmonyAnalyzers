# MHA010: `HarmonyPatch` attributes conflict

While the `HarmonyPatch` attribute can be specified multiple times on a single class or method, only one value for each argument will be applied.

## Examples

### Valid usage of multiple attributes

```cs
[HarmonyPatch(typeof(TargetType))]
[HarmonyPathc(nameof(TargetType.TargetMethod))]
static class PatchClass
{
    [HarmonyPostfix]
    [HarmonyPatch(new Type[] { typeof(string) })]
    static void PatchFirstOverload()
    {
        //...
    }

    [HarmonyPostfix]
    [HarmonyPatch(new Type[] { typeof(string) })]
    static void PatchSecondOverload()
    {
        //...
    }
}
```

### Conflicting attributes

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Prefix()
    {
        //...
    }

    static void Postfix()
    {
        //...
    }

    [HarmonyPostfix]
    // These conflict with each other and with the class attribute
    [HarmonyPatch(nameof(TargetType.SecondTargetMethod))]
    [HarmonyPatch(nameof(TargetType.ThirdTargetMethod))]
    static void PatchOtherMethods()
    {

    }
}

```