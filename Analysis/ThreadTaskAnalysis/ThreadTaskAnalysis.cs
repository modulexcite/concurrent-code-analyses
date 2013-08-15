using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utilities;

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

        protected override bool FilterProject(Enums.ProjectType type)
        {
            return true;
        }

        protected override void VisitDocument(Document document, SyntaxNode root)
        {
            var loopWalker = new ThreadTaskAnalysisWalker(Result)
            {
                Outer = this,
                Namespace = "System.Threading.Tasks",
                SemanticModel = (SemanticModel) document.GetSemanticModelAsync().Result,
                Id = Result.AppName + " " + document.Id
            };

            loopWalker.Visit(root);
        }

        protected override bool FilterDocument(Document doc)
        {
            return true;
        }
    }
}