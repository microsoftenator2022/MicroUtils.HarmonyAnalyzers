# MHA012: `__result` injection in passthrough Postfix

Passthrough postfix patch methods' first argument is always the injected result of the target method. A `__result` injection is unnecessary.

## Examples

Unnecessary `__result` injection

```cs
[HarmonyPostfix]
string PatchMethod(string s, string __result)
{
    ///...
}
```

Using `__result` as the name of the first parameter is valid

```cs
[HarmonyPostfix]
string PatchMethod(string __result)
{
    ///...
}
```
