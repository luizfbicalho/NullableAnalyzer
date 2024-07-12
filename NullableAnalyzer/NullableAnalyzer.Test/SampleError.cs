using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NullableAnalyzer.Test
{
    class TypeName
    {
        public void Method()
        {

            string? test = null;
            var x1 = test?.ToString();
            if (test != null)
            {
                var x2 = test?.ToArray();
                var x3 = test!.ToArray();

            }
        }
    }
}
