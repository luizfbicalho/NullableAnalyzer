using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace NullableAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullableAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NULLABLE0001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ConditionalAccessExpression, SyntaxKind.SuppressNullableWarningExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode expression = null;
            string operatorUsed = "";

            switch (context.Node)
            {
                case ConditionalAccessExpressionSyntax conditionalAccess:
                    expression = conditionalAccess.Expression;
                    operatorUsed = "?.";
                    break;
                case PostfixUnaryExpressionSyntax postfixUnary when postfixUnary.IsKind(SyntaxKind.SuppressNullableWarningExpression):

                    expression = postfixUnary.Operand;
                    operatorUsed = "!.";
                    break;
            }

            if (expression != null)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(expression);
                var symbolInfo = context.SemanticModel.GetSymbolInfo(expression);

                // Perform flow analysis to determine nullability in context
                var flowState = context.SemanticModel.AnalyzeNullability(expression, context.CancellationToken);

                // Check if the expression is a non-nullable reference type or a non-nullable value type
                if ((typeInfo.Type?.IsReferenceType == true && flowState == NullableFlowState.NotNull) ||
                    (typeInfo.Type?.IsValueType == true && !typeInfo.Type.IsNullableValueType()))
                {
                    var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), operatorUsed);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }


    }
}
