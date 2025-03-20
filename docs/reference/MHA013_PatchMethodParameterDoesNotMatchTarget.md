# MHA013: Patch method parameter does not match target

For prefix, postfix and finalizer patches, patch method arguments must either be  "`__`" injecctions or must match the names of arguments on the target method.

### Example Violation

`stringArg` does not match the name of any parameter on `TargetMethod`

```cs
class TargetType
{
    public string TargetMethod(string s, int i)
    {
        //...
    }
}

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static bool Prefix(string stringArg, out string __result)
    {
        //...
    }
}
```

### Fix

Use matching argument name

```cs
static bool Prefix(string s, out string __result)
```