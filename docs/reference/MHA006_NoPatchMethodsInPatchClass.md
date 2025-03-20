# MHA006: No patch methods in patch class

A patch class should have at least one patch method.

## Example Violations

Class with no patch methods

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
class ClassWithNoPatches
{
    void NonPatchMethod() {}
}
```

Class with misnamed patch method

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    // Should be 'Postfix'
    void PostFix() {}
}
```

## Related rules

[MHA019: Patch type method name violation ](MHA019_PatchTypeMethodNameViolation.md)