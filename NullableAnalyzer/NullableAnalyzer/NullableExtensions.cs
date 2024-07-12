using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;

namespace NullableAnalyzer
{
    internal static class NullableExtensions
    {
        public static bool IsNullable(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.IsReferenceType || typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        }

        public static NullableFlowState? AnalyzeNullability(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            SyntaxNode enclosingBlock = node.FirstAncestorOrSelf<BlockSyntax>();
      

            // Attempt to cast the SyntaxNode to an ExpressionSyntax.
            // Only ExpressionSyntax nodes can have a nullability state.
            if (node is ExpressionSyntax expression )
            {
                // Proceed with the analysis as before, now using the expression variable.
                var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

                // If the type is a value type and not a nullable value type, it cannot be null.
                if (typeInfo.Type?.IsValueType == true && !typeInfo.Type.IsNullableValueType())
                {
                    return NullableFlowState.NotNull;
                }

                // For reference types or nullable value types, analyze the data flow to determine if the variable might be null.
                var dataFlow = semanticModel.AnalyzeDataFlow(enclosingBlock ?? expression);
                if (dataFlow.Succeeded)
                {
                    // If the expression is a simple identifier, we can check if it's definitely assigned and not null.
                    if (expression is IdentifierNameSyntax identifierName)
                    {
                        var symbol = semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol;
                        if (dataFlow.AlwaysAssigned.Contains(symbol) && !dataFlow.DataFlowsIn.Contains(symbol))
                        {
                            // Assuming the variable is not null if it's always assigned and there's no data flow into it.
                            // This is a simplification and might not always be accurate.
                            return NullableFlowState.NotNull;
                        }
                    }
                }

                // This is a simplified default assumption. In reality, determining nullability accurately can be much more complex.
                return NullableFlowState.MaybeNull;
            }

            // If the node is not an ExpressionSyntax, return null to indicate that nullability analysis is not applicable.
            return null;
        }


        public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol namedTypeSymbol &&
                   namedTypeSymbol.IsGenericType &&
                   namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
        }
    }
}
