using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace EnumeratorCallAnalyzer
{
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class EnumeratorCallAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EnumeratorCallAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create( Rule ); } }

        public override void Initialize( AnalysisContext context )
        {
            context.EnableConcurrentExecution( );
            context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze|GeneratedCodeAnalysisFlags.ReportDiagnostics );

            // The AnalyzeNode method will be called for each InvocationExpression of the Syntax tree
            context.RegisterSyntaxNodeAction( _CheckCoroutineCall, SyntaxKind.InvocationExpression );
        }

        private void _CheckCoroutineCall( SyntaxNodeAnalysisContext context )
        {
            var fcall = (InvocationExpressionSyntax)context.Node;

            // Filter only bare function call
            // like: Coroutine( );
            // not: yield return Coroutine( );
            // not: var generator = Coroutine( );
            var parent = fcall.Parent;

            // The bare call will only have
            // - ExpressionStatementSyntax
            //   - InvocationExpressionSyntax
            //   - SemicolonToken
            bool isBareCall = (parent is ExpressionStatementSyntax) && parent.ChildNodes( ).Count( ) == 1;
            if( !isBareCall )
                return; // Skip, it is not bare

            // Check if function has returning type of something based off IEnumerator
            var invokedMethod = context.SemanticModel.GetSymbolInfo( fcall ).Symbol as IMethodSymbol; // Since this is in InvocationExpressionSyntax, it is pretty sure a IMethodSymbol
            if( invokedMethod == null )
                return;

            // Lucky that IEnumerator can be checked with SpecialType.System_Collections_IEnumerator. No crazy type crawling here.
            if( invokedMethod.ReturnType.SpecialType == SpecialType.System_Collections_IEnumerator )
            {
                var diagnostic = Diagnostic.Create(Rule, fcall.Parent.GetLocation(), invokedMethod.Name ); // messageArg will fill the '{0}' part in analyzer format
                context.ReportDiagnostic( diagnostic );
            }
        }
    }

    public class Test
    {
        IEnumerator CallingSite()
        {
            // Bare call (bad)
            Coroutine(); // Markup to tell test that this line must trigger the analyzer

            yield return Coroutine();
        }

        IEnumerator Coroutine()
        {
            yield return null;
        }
    }
}
