namespace MicroUtils.HarmonyAnalyzers;

using Microsoft.CodeAnalysis;

using static DiagnosticId;

public enum RuleCategory
{
    PatchAttribute,
    TargetMethod,
    PatchMethod
}

public enum DiagnosticId
{
    MHA001,
    MHA002,
    MHA003,
    MHA004,
    MHA005,
    MHA006,
    MHA007,
    MHA008,
    MHA009,
    MHA010,
    MHA011,
    MHA012,
    MHA013,
    MHA014,
    MHA015,
    MHA016,
    MHA017,
    MHA018,
    MHA019
}

internal static class ReferenceDoc
{
    public const string UriRoot = "https://github.com/microsoftenator2022/MicroUtils.HarmonyAnalyzers/tree/master/docs/reference";
    public static string GetUriString(string referenceDocPath) => string.Join("/", UriRoot, referenceDocPath);

    public static Optional<string> GetUriString(DiagnosticId id) => (id switch
    {
        MHA001 => "MHA001_MissingHarmonyPatchClassAttribute.md",
        MHA002 => "MHA002_MissingPatchTypeAttribute.md",
        MHA003 => "MHA003_MissingMethodTypeargumentForHarmonyPatchAttribute.md",
        MHA004 => "MHA004_AmbiguousMatch.md",
        MHA005 => "MHA005_HarmonyPatchTargetMethodNotFound.md",
        MHA006 => "MHA006_NoPatchMethodsInPatchClass.md",
        MHA007 => "MHA007_MultipleTargetMethodDefinitionsForPatchClass.md",
        MHA008 => "MHA008_AssignmentToNonRefPatchMethodArgument.md",
        MHA009 => "MHA009_InvalidPatchMethodReturnType.md",
        MHA010 => "MHA010_HarmonypatchAttributesConflict.md",
        MHA011 => "MHA011_PatchTypeConflict.md",
        MHA012 => "MHA012_ResultInjectionInPassthroughPostfix.md",
        MHA013 => "MHA013_PatchMethodParameterDoesNotMatchTarget.md",
        MHA014 => "MHA014_InvalidInjectionParameterType.md",
        MHA015 => "MHA015_InvalidTranspilerMethodParameter.md",
        MHA016 => "MHA016_UseOutModifierForStateParameterInPrefix.md",
        MHA017 => "MHA017_UseParameterNameOverIndexInjection.md",
        MHA018 => "MHA018_ReversePatchSignatureDoesNotMatchTargetMethod.md",
        MHA019 => "MHA019_MethodNameCasingViolationPatchType.md",
        _ => default(Optional<string>)
    })
    .Select(GetUriString);
}