# MHA008: Assignment to non-ref patch method argument

Assignment can only affect the value passed to the target method if the corresponding injection on the patch method is `ref`.

## Example Violation

Assignment to non-`ref` injected parameter `i`

```cs
class TargetType
{
    public int AddOne(int i) => i + 1;
}

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Prefix(int i)
    {
        i = 0;
    }
}
```

### Fix

```cs
[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Prefix(ref int i)
    {
        i = 0;
    }
}
```