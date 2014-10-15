using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Configuration;
using Utilities;
using Analysis;

namespace ConsultingAnalysis
{
    public class ConsultingAnalysis : AnalysisBase
    {
        private ConsultingAnalysisResult result;

        public override AnalysisResultBase ResultObject
        {
            get { return result; }
        }

        public new ConsultingAnalysisResult Result
        {
            get { return result; }
        }

        public ConsultingAnalysis(string solutionPath, string appName)
            : base(solutionPath, appName)
        {
            result = new ConsultingAnalysisResult(solutionPath, appName);
        }

        protected override bool FilterProject(Enums.ProjectType type)
        {
            return true;
        }

        protected override void VisitDocument(Document document, SyntaxNode root)
        {
            CSharpSyntaxWalker walker;
            SemanticModel semanticModel = (SemanticModel)document.GetSemanticModelAsync().Result;


            if (bool.Parse(ConfigurationManager.AppSettings["IsCPUAsyncDetectionEnabled"]))
            {
                walker = new CPUAsyncDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }

            if (bool.Parse(ConfigurationManager.AppSettings["IsAsyncAwaitDetectionEnabled"]))
            {
                walker = new AsyncAwaitDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }

            if (bool.Parse(ConfigurationManager.AppSettings["IsComplexPatternDetectionEnabled"]))
            {
                walker = new ComplexPatternDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }

            if (bool.Parse(ConfigurationManager.AppSettings["IsAsyncLibraryDetectionWalkerEnabled"]))
            {
                walker = new AsyncLibraryDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            
        }

        protected override bool FilterDocument(Document doc)
        {
            return true;
        }
    }
}