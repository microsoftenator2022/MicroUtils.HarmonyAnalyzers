# MHA005: HarmonyPatch target method not found

Occurs when a target method is not specified or is not found.

## Example Violation

```cs

class TargetType
{
    public void SomeMethod() {}
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetType), "NonexistentMethod")]
    static void Postfix() {}
}
```

### Fix

Provide `HarmonyPatch` attribute arguments that match a method on the target type

```cs
[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetType), "SomeMethod")]
    static void Postfix() {}
}
```

## Related rules

[MHA003: Missing `MethodType` argument to `[HarmonyPatch]` attribute](MHA003_MissingMethodTypeArgumentForHarmonyPatchAttribute.md)

[MHA004: Ambiguous Match](MHA004_AmbiguousMatch.md)