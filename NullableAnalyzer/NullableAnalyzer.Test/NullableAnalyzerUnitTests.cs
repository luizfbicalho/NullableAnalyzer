using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = NullableAnalyzer.Test.CSharpCodeFixVerifier<
    NullableAnalyzer.NullableAnalyzerAnalyzer,
    NullableAnalyzer.NullableAnalyzerCodeFixProvider>;

namespace NullableAnalyzer.Test
{
    [TestClass]
    public class NullableAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Method()
            {

                string? test = null;
                var x1 = test?.ToString();
                if(test != null)
                {
                    var x2 = test?.ToArray();
                    var x3 = test!.ToArray();

                }
            }
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Method()
            {

                string? test = null;
                var x1 = test?.ToString();
                if(test != null)
                {
                    var x2 = test.ToString();
                }
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("NULLABLE0001").WithNoLocation();
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
