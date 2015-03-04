using Analysis;
using Microsoft.CodeAnalysis;

namespace AnalysisRunner
{
    public class AsyncAwaitUsageAnalysis : BaseAnalysis
    {
        public AsyncAwaitUsageAnalysis(Document sourceFile, SemanticModel semanticModel)
        {
            SourceFile = sourceFile;
            SemanticModel = semanticModel;
            Result = new AsyncAwaitUsageResult();
        }


    }

    public class AsyncAwaitUsageResult : AnalysisResult
    {

        public AsyncAwaitUsageResult()
        {
            Type = AnalysisType.AsyncAwaitUsage;
        }
    }
}