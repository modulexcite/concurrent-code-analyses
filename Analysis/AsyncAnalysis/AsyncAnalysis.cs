using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Utilities;


namespace Analysis
{
    public class AsyncAnalysis : AnalysisBase
    {
        private AsyncAnalysisResult result;

        private List<String> AnalyzedMethods;

        private Dictionary<String,int> AnalyzedMethodsDict;

        public override AnalysisResultBase ResultObject
        {
            get { return result; }
        }

        public new AsyncAnalysisResult Result
        {
            get { return result; }
        }

        public AsyncAnalysis(string dirName, string appName)
            : base(dirName, appName)
        {
            result = new AsyncAnalysisResult(appName);
            AnalyzedMethods = new List<String>();
            AnalyzedMethodsDict = new Dictionary<string, int>();
        }

        protected override bool FilterProject(Enums.ProjectType type)
        {
            if (type == Enums.ProjectType.WP7 || type == Enums.ProjectType.WP8)
            {
                //Result.WritePhoneProjects();
                return true;
            }
            return false;
        }

        protected override void VisitDocument(Document document, SyntaxNode root)
        {
            SyntaxWalker walker;
            SemanticModel semanticModel = (SemanticModel)document.GetSemanticModelAsync().Result;

            if (bool.Parse(ConfigurationManager.AppSettings["IsGeneralAsyncDetectionEnabled"]))
            {
                walker = new GeneralAsyncDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsAsyncUsageDetectionEnabled"]))
            {
                walker = new AsyncUsageDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document, IsEventHandlerWalkerEnabled = false };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsSyncUsageDetectionEnabled"]))
            {
                walker = new SyncUsageDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsAPMDiagnosisDetectionEnabled"]))
            {
                walker = new APMDiagnosisDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["IsAsyncAwaitDetectionEnabled"]))
            {
                walker = new AsyncAwaitDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document, AnalyzedMethods = AnalyzedMethods };
                walker.Visit(root);
            }
            if (bool.Parse(ConfigurationManager.AppSettings["DispatcherDetectionEnabled"]))
            {
                walker = new DispatcherDetectionWalker { Result = Result, SemanticModel = semanticModel, Document = document, AnalyzedMethods = AnalyzedMethodsDict };
                walker.Visit(root);
            }
        }

        protected override bool FilterDocument(Document doc)
        {
            if (Path.GetDirectoryName(doc.FilePath).Contains(@"\Service References\"))
                return false;

            return true;
        }
    }
}