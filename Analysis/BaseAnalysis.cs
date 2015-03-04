using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analysis
{
    public enum AnalysisType { AsyncAwaitUsage, AsyncAwaitMisusage, CPUAsyncUsage, IOAsyncUsage };

    public abstract class BaseAnalysis : CSharpSyntaxWalker
    {
        public SemanticModel SemanticModel { get; set; }
        public Document SourceFile { get; set; }
        public AnalysisResult Result { get; set; }
    }

    public abstract class AnalysisResult
    {
        public AnalysisType Type { get; set; }
    }
}
