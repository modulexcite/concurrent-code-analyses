using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace Analysis
{
    public class ThreadTaskAnalysis : AnalysisBase
    {

        private ThreadTaskAnalysisResult result;

         public override AnalysisResultBase ResultObject
        {
            get { return result; }
        }
        public new ThreadTaskAnalysisResult Result
        {
            get { return result; }
        }
        public ThreadTaskAnalysis(string dirName, string appName)
            : base(dirName, appName)
        {
             result = new ThreadTaskAnalysisResult(appName);
        }
        

        protected override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();

            var loopWalker = new ThreadTaskAnalysisWalker(Result)
            {
                Outer = this,
                Namespace = "System.Threading.Tasks",
                SemanticModel = document.GetSemanticModel(),
                Id = Result._appName + " " + document.Id
            };

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

    }
}
