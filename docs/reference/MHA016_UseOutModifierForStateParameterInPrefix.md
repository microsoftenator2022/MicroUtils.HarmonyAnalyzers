# MHA016: Use out modifier for `__state` parameter in prefix

The `__state` provided to a prefix patch will always<sup>[note](#note)</sup> have default value and should use `out` and not `ref`.

## Example Violation

```cs
static void Prefix(ref object __state)
```

### Fix

```cs
static void Prefix(out object __state)
```

<a id="note"><sup>note</sup></a> It is possible to set `__state` with another prefix in the same patch class, and suppressing the rule is correct in this case.