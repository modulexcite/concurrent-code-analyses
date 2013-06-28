using System.IO;
using Roslyn.Compilers.CSharp;

namespace Analysis
{
    class InterestingCallsCollector
    {
        private readonly StreamWriter _writer;

        public InterestingCallsCollector(StreamWriter writer)
        {
            _writer = writer;
        }

        
    }
}
