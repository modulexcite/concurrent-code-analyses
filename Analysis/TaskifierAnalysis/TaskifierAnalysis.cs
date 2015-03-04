using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Configuration;
using Utilities;

namespace Analysis
{
    public class TaskifierAnalysis : AnalysisBase
    {
        private TaskifierAnalysisResult result;

        public override AnalysisResult ResultObject
        {
            get { return result; }
        }

        public new TaskifierAnalysisResult Result
        {
            get { return result; }
        }

        public TaskifierAnalysis(string solutionPath, string appName)
            : base(solutionPath, appName)
        {
            result = new TaskifierAnalysisResult(solutionPath, appName);
        }

        protected override bool FilterProject(Enums.ProjectType type)
        {
            return true;
        }

        protected override void VisitDocument(Document document, SyntaxNode root)
        {
            CSharpSyntaxWalker walker;
            SemanticModel semanticModel = (SemanticModel)document.GetSemanticModelAsync().Result;


            if (bool.Parse(ConfigurationManager.AppSettings["IsGeneralTaskifierDetectionEnabled"]))
            {
                walker = new GeneralTaskifierDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsThreadUsageDetectionEnabled"]))
            {
                walker = new ThreadUsageDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsTasksUsageDetectionEnabled"]))
            {
                walker = new TasksUsageDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsSimplifierDetectionEnabled"]))
            {
                walker = new SimplifierDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
        }

        protected override bool FilterDocument(Document doc)
        {
            return true;
        }
    }
}