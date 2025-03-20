# MHA014: Invalid injection parameter type

Injection parameters (`__result`, `__instance`, etc.) should be assignable from the correct type.

## Example Violation

```cs
class TargetType
{
    public string TargetMethod()
    {
        //...
    }
}

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class PatchClass
{
    static void Postfix(bool __result)
    {
        //...
    }
}
```

### Fix

Either of these is valid:

```cs
static void Postfix(string __result)
```
```cs
static void Postfix(object __result)
```

## Injection argument types

Injection           | Type 
--------------------|------------------------------------------
 `__args`           | `object[]`
 `__exception`      | `Exception`  
 `__instance`       | Containing type of target method
 `__originalMethod` | `MethodBase`
 `__result`         | Return type of target method
 `__runOriginal`    | `bool`
