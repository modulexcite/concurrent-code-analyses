using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace Analysis
{
    public class ThreadTaskProjectAnalysis : ProjectAnalysisBase
    {
        private readonly ThreadTaskProjectAnalysisSummary _summary;

        public ThreadTaskProjectAnalysis(string dirName, ThreadTaskProjectAnalysisSummary summary)
            : base(dirName, summary)
        {
            _summary = summary;
        }

        protected override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();

            var loopWalker = new ThreadTaskProjectAnalysisWalker(_summary)
            {
                Outer = this,
                Namespace = "System.Threading.Tasks",
                SemanticModel = document.GetSemanticModel(),
                Id = _summary.AppName + " " + document.Id
            };

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        protected override void OnAnalysisCompleted()
        {
            _summary.WriteResults();
        }
    }
}
