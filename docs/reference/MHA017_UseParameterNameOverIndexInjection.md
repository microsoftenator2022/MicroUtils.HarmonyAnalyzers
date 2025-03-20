# MHA017: Use parameter name over index injection

Where possible, patch methods should use named parameter injections (`ArgType argName`) over indexed injections (`ArgType __n`)

## Example Violation

```cs
class TargetType
{
    public void TargetMethod(int arg1)
    {
        //...
    }
}

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Prefix(int __0)
    {
        //...
    }
}
```

### Fix

```cs
static void Prefix(int arg1)
```
