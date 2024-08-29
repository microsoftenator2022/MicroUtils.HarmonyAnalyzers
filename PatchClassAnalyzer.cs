﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using HarmonyAnalyzers.Rules;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HarmonyAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class PatchClassAnalyzer : DiagnosticAnalyzer
{

#if DEBUG
    private static readonly DiagnosticDescriptor DebugMessage = new(
#pragma warning disable RS2000 // Add analyzer diagnostic IDs to analyzer release
        "DEBUG",
#pragma warning restore RS2000 // Add analyzer diagnostic IDs to analyzer release
        "Debug message",
        "{0}",
        "Debug",
        DiagnosticSeverity.Info,
        true);
#endif

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
#if DEBUG
        DebugMessage,
#endif
        MissingClassAttribute.Descriptor,
        MissingPatchTypeAttribute.Descriptor,
        MissingMethodType.Descriptor,
        AmbiguousMatch.Descriptor,
        TargetMethodMatchFailed.Descriptor,
        NoPatchMethods.Descriptor,
        MultipleTargetMethodDefinitions.Descriptor
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax cds)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(cds, context.CancellationToken) is not INamedTypeSymbol classSymbol)
            return;

        if (HarmonyTypeHelpers.GetHarmonyPatchType(context.Compilation, context.CancellationToken) is not { } harmonyAttribute)
            return;

        var patchTypeAttributeTypes =
            HarmonyTypeHelpers.GetHarmonyPatchMethodAttributeTypes(context.Compilation, context.CancellationToken).ToImmutableArray();

        var classAttributes = classSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Equals(harmonyAttribute, SymbolEqualityComparer.Default) ?? false)
            .ToImmutableArray();

        var patchMethodsData = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m =>
            {
                var attrs = m.GetAttributes()
                    .Where(attr => attr.AttributeClass is { } type && type.Equals(harmonyAttribute, SymbolEqualityComparer.Default))
                    .ToImmutableArray();

                return (m, attrs);
            })
            .Where(static pair => pair.attrs.Length > 0 || Constants.HarmonyPatchTypeNames.Contains(pair.m.Name))
            .Select(pair => new PatchMethodData(classSymbol, pair.m).AddTargetMethodData(classAttributes).AddTargetMethodData(pair.attrs))
            .ToImmutableArray();

        if (classAttributes.Length == 0 && patchMethodsData.Length == 0)
            return;

        MissingClassAttribute.Check(context, classSymbol, classAttributes, patchMethodsData);
        NoPatchMethods.Check(context, classSymbol, classAttributes, patchMethodsData);

        ImmutableArray<INamedTypeSymbol?> targetMethodsAttributeTypes =
        [
            HarmonyTypeHelpers.GetHarmonyTargetMethodType(context.Compilation, context.CancellationToken),
            HarmonyTypeHelpers.GetHarmonyTargetMethodsType(context.Compilation, context.CancellationToken)
        ];

        var targetMethodMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m =>
                m.Name is Constants.TargetMethodMethodName or Constants.TargetMethodsMethodName ||
                m.GetAttributes().Any(attr => targetMethodsAttributeTypes.Contains(attr.AttributeClass, SymbolEqualityComparer.Default)))
            .ToImmutableArray();

#if DEBUG
        foreach (var patchMethodData in patchMethodsData)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            var patchMethod = patchMethodData.PatchMethod;

            context.ReportDiagnostic(Diagnostic.Create(
            descriptor: DebugMessage,
            location: patchMethod.Locations[0],
            messageArgs: patchMethodData));
        }
#endif

        foreach (var patchMethod in patchMethodsData)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;
            
            MissingPatchTypeAttribute.Check(context, patchMethod.PatchMethod, patchTypeAttributeTypes, patchMethod.PatchMethod.Locations);
        }

        if (targetMethodMethods.Length > 0)
        {
            MultipleTargetMethodDefinitions.Check(context, classSymbol, classAttributes, patchMethodsData, targetMethodMethods);
            return;
        }
        else
        {
            foreach (var patchMethodData in patchMethodsData)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var patchMethod = patchMethodData.PatchMethod;

                if (patchMethodData.TargetMethod is not null)
                {
                    continue;
                }

                if(MissingMethodType.Check(context, patchMethodData, patchMethod.Locations))
                    continue;

                if (AmbiguousMatch.Check(context, patchMethodData, patchMethod.Locations))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: TargetMethodMatchFailed.Descriptor,
                    location: patchMethod.Locations[0],
                    additionalLocations: patchMethod.Locations.Skip(1),
                    messageArgs: patchMethod));
            }
        }
    }
}
