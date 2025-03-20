# MHA019: Method name casing violation (patch type)

Incorrect case for patch method name.

## Example Violation

```cs
static void PostFix()
{
    //...
}
```

### Expected

```cs
static void Postfix()
{

}