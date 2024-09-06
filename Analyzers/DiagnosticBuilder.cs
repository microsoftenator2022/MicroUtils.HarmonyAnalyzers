using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MicroUtils.HarmonyAnalyzers;
internal record class DiagnosticBuilder(DiagnosticDescriptor Descriptor)
{
    public Location? PrimaryLocation { get; init; } = null;
    public DiagnosticSeverity? EffectiveSeverity { get; init; } = null;
    public ImmutableArray<Location> AdditionalLocations { get; init; } = [];
    public ImmutableDictionary<string, string?> Properties { get; init; } = ImmutableDictionary<string, string?>.Empty;
    public ImmutableArray<object?> MessageArgs { get; init; } = [];
    
    public Diagnostic Create()
    {
        if (this.PrimaryLocation is null)
            throw new InvalidOperationException($"{nameof(DiagnosticBuilder.PrimaryLocation)} is not set");

        if (this.EffectiveSeverity is { } effectiveSeverity)
            return Diagnostic.Create(
                descriptor: this.Descriptor,
                location: this.PrimaryLocation,
                effectiveSeverity: effectiveSeverity,
                properties: this.Properties,
                additionalLocations: this.AdditionalLocations,
                messageArgs: this.MessageArgs.ToArray());

        return Diagnostic.Create(
            descriptor: this.Descriptor,
            location: this.PrimaryLocation,
            properties: this.Properties,
            additionalLocations: this.AdditionalLocations,
            messageArgs: this.MessageArgs.ToArray());
    }

    public IEnumerable<DiagnosticBuilder> ForAllLocations(ImmutableArray<Location> locations)
    {
        foreach (var location in locations)
            yield return this with { PrimaryLocation = location };
    }

    public IEnumerable<DiagnosticBuilder> ForAllLocations(params Location[] locations) =>
        this.ForAllLocations(locations.ToImmutableArray());
}

internal static class DiagnosticExtensions
{
    public static void ReportAll(this SyntaxNodeAnalysisContext context, IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            context.ReportDiagnostic(diagnostic);
        }
    }

    public static ImmutableArray<Diagnostic> CreateAll(this IEnumerable<DiagnosticBuilder> builders) =>
        builders.Select(builder => builder.Create()).ToImmutableArray();
}
