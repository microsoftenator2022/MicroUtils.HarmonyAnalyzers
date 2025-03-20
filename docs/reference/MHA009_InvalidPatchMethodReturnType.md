# MHA009: Invalid return type

Harmony methods in a patch class method's return type must be assignable to a valid type.

## Examples

### Invalid return type

```cs
[HarmonyPrefix]
static string PrefixPatch()
{
    //...
}
```

### Valid return type

```cs
[HarmonyPrefix]
static void PrefixPatch()
{
    //...
}
```

## Valid patch method return types

### Postfix
- `void`
- Target method's return type (passthrough only)

### Prefix
- `void`
- `bool`

### Finalizer
- `void`
- `Exception`

### Transpiler
- `IEnumerable<CodeInstruction>`

### `TargetMethod`
- `MethodBase`

### `TargetMethods`
- `IEnumerable<MethodBase>`
