using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EnumeratorCallAnalyzer
{
    [ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( EnumeratorCallAnalyzerCodeFixProvider ) ), Shared]
    public class EnumeratorCallAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create( EnumeratorCallAnalyzerAnalyzer.DiagnosticId ); }
        }

        public sealed override FixAllProvider GetFixAllProvider( )
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync( CodeFixContext context )
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => PrependYieldReturnAsync( context.Document, root, declaration, c ),
                    equivalenceKey: nameof( CodeFixResources.CodeFixTitle ) ),
                diagnostic );
        }

        private Task<Document> PrependYieldReturnAsync( Document document, SyntaxNode root, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken )
        {
            // document in this context is what code span reported from analyzer
            // It would be 'StatementSyntax' level on top of InvocationExpressionSyntax

            // From:
            // Coroutine( );
            // - ExpressionStatementSyntax <- replacingPart
            //   - InvocationExpressionSyntax <- invocationExpr
            //   - SemicolonToken

            // We want:
            // yield return Coroutine( );
            // - YieldReturnStatementSyntax 
            //   - InvocationExpressionSyntax <- invocationExpr
            //   - SemicolonToken
            
            var replacingPart = invocationExpr.Parent;

            YieldStatementSyntax yieldRoot = SyntaxFactory.YieldStatement( SyntaxKind.YieldReturnStatement, invocationExpr.WithoutTrivia( ) )
                .WithLeadingTrivia( invocationExpr.GetLeadingTrivia( ) ) // These are required to retain leading/trailing comments
                .WithTrailingTrivia( replacingPart.GetTrailingTrivia( ) );

            yieldRoot = yieldRoot.WithAdditionalAnnotations( Formatter.Annotation );
            var newRoot = root.ReplaceNode( replacingPart, yieldRoot ); // Finally substitution
            return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
        }
    }
}
