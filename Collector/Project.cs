using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.IO;
using Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Exceptions;
using RoslynUtilities;
using Microsoft.CodeAnalysis.CSharp;

namespace AnalysisRunner
{
    public class Project
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public Enums.ProjectType Type { get; set; }
        public int SLOC { get; set; }
        public bool IsAnalyzed { get; set; }
        public List<AnalysisResult> AnalysisResults { get; set; }

        [NotMapped]
        private MSBuildWorkspace workspace { get; set; }

        public void Analyze()
        {
            workspace = MSBuildWorkspace.Create();
            try
            {
                var project = workspace.OpenProjectAsync(Path).Result;
                if (!project.HasDocuments && !project.IsCSharpProject())
                {
                    return;
                }
                
                Type = project.GetProjectType();

                foreach (var document in project.Documents)
                {
                    AnalyzeSourceFile(document);
                }
                IsAnalyzed = true;
            }
            catch (Exception ex)
            {
                IsAnalyzed = false;
                if (ex is InvalidProjectFileException ||
                    ex is FormatException ||
                    ex is ArgumentException ||
                    ex is PathTooLongException ||
                    ex is AggregateException)
                {
                    Logs.ErrorLog.Info("Project not analyzed: {0}: Reason: {1}", Path, ex.Message);
                }
                else
                    throw;
            }
            finally
            {
                workspace.Dispose();
            }
        }

        private void AnalyzeSourceFile(Document sourceFile)
        {
            var root = (SyntaxNode)sourceFile.GetSyntaxRootAsync().Result;
            var semanticModel = (SemanticModel)sourceFile.GetSemanticModelAsync().Result;
            var sloc = sourceFile.GetTextAsync().Result.Lines.Count;

            try
            {
                PerformAnalysisTypes(sourceFile, root, semanticModel);
                SLOC += sloc;
            }
            catch (InvalidProjectFileException ex)
            {
                Logs.ErrorLog.Info("SourceFile is not analyzed: {0}: Reason: {1}", sourceFile.FilePath, ex.Message);
            }
        }

        private void PerformAnalysisTypes(Document sourceFile, SyntaxNode root, SemanticModel semanticModel)
        {
            BaseAnalysis walker=null;
            foreach (var analysisType in Runner.AnalysisTypes)
            {
                switch (analysisType)
                {
                    case AnalysisType.AsyncAwaitUsage:
                        walker = new AsyncAwaitUsageAnalysis(sourceFile, semanticModel);
                        walker.Visit(root);
                        break;
                    default:
                        walker = null;
                        break;
                }
                AnalysisResults.Add(walker.Result);
            }
        }
    }
}