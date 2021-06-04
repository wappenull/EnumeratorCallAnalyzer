using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = EnumeratorCallAnalyzer.Test.CSharpCodeFixVerifier<
    EnumeratorCallAnalyzer.EnumeratorCallAnalyzerAnalyzer,
    EnumeratorCallAnalyzer.EnumeratorCallAnalyzerCodeFixProvider>;

namespace EnumeratorCallAnalyzer.Test
{
    [TestClass]
    public class EnumeratorCallAnalyzerUnitTest
    {
        const string BadCase = @"
using System;
using System.Collections;
public class Test
{
    IEnumerator CallingSite()
    {
        // Comment before
        [|Coroutine();|] // Comment after
        yield return null;
    }

    IEnumerator Coroutine()
    {
        yield return null;
    }
}";

        //No diagnostics expected to show up
        [TestMethod]
        public async Task BareCase_Diagnostic( )
        {
            await VerifyCS.VerifyAnalyzerAsync( BadCase );
        }

        [TestMethod]
        public async Task GoodCase_NoDiagnostic( )
        {
            var test = @"
using System;
using System.Collections;
public class Test
{
    IEnumerator CallingSite()
    {
        // Good calls
        yield return Coroutine();
        var generator = Coroutine();
    }

    IEnumerator Coroutine()
    {
        yield return null;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync( test );
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task CodeFixText( )
        {
            var expectedFix = @"
using System;
using System.Collections;
public class Test
{
    IEnumerator CallingSite()
    {
        // Comment before
        yield return Coroutine(); // Comment after
        yield return null;
    }

    IEnumerator Coroutine()
    {
        yield return null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync( BadCase, expectedFix );
        }
    }
}
