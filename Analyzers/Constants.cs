using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;

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
    MHA015
}
