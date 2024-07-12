using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NullableAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableAnalyzerCodeFixProvider)), Shared]
    public class NullableAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NullableAnalyzerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the syntax node on which to fix the diagnostic.
            var syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => RemoveUnnecessaryOperatorAsync(context.Document, syntaxNode, c),
                    equivalenceKey: CodeFixResources.CodeFixTitle),
                diagnostic);
        }

        private async Task<Document> RemoveUnnecessaryOperatorAsync(Document document, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Depending on the node type, remove or replace the operator.
            switch (syntaxNode)
            {
                case ConditionalAccessExpressionSyntax conditionalAccess:
                    // Replace the conditional access with its expression.
                    editor.ReplaceNode(conditionalAccess, conditionalAccess.Expression.WithTriviaFrom(conditionalAccess));
                    break;
                case PostfixUnaryExpressionSyntax postfixUnary when postfixUnary.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                    // Remove the null-forgiving operator.
                    editor.ReplaceNode(postfixUnary, postfixUnary.Operand.WithTriviaFrom(postfixUnary));
                    break;
            }

            return editor.GetChangedDocument();
        }
    }
}
