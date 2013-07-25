using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analysis;

namespace Test
{
    class Test4
    {


        public static void execute()
        {
            var app = new AsyncAnalysis(@"Z:\C#PROJECTS\PhoneApps\codetitans", "test");

            app.Analyze();
            Console.ReadLine();
        }
    }
}
