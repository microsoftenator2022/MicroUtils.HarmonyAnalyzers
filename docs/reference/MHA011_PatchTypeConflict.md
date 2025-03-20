# MHA011: Patch type conflict

A single patch method can only be applied as one patch type.

## Example Violations

```cs
[HarmonyPrefix]
[HarmonyPostfix]
static void PatchMethod()
{
    //...
}
```
```cs
[HarmonyPrefix]
static void Postfix()
{
    //...
}
```

### Fix

Define patch methods with a single patch type
```cs
[HarmonyPrefix]
static void PatchMethod()
{
    //...
}
```
```cs
static void Postfix()
{
    //...
}
```