using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

internal interface IPatchRule
{
    DiagnosticDescriptor Descriptor { get; }
}
