using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Xml;
using NLog;

namespace Utilities
{
    /// <summary>
    /// Several handy extension methods for Roslyn types.
    /// </summary>
    public static class RoslynExtensions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool IsCSProject(this Project project)
        {
            return project.Language.Equals("C#");
        }

        public static int CountSLOC(this SyntaxNode node)
        {
            var text = node.GetText();
            var totalLines = text.Lines.Count();

            var linesWithNoText = 0;
            foreach (var l in text.Lines)
            {
                if (String.IsNullOrEmpty(l.ToString().Trim()))
                {
                    ++linesWithNoText;
                }
            }
            return totalLines - linesWithNoText; ;
        }

        public static Enums.ProjectType GetProjectType(this Project project)
        {
            var result = project.IsWindowsPhoneProject();
            if (result == 1)
                return Enums.ProjectType.WP7;
            else if (result == 2)
                return Enums.ProjectType.WP8;
            else if (project.IsNet40Project())
                return Enums.ProjectType.NET4;
            else if (project.IsNet45Project())
                return Enums.ProjectType.NET45;
            else
                return Enums.ProjectType.NETOther;
        }

        public static string ToStringWithReturnType(this MethodSymbol symbol)
        {
            var methodCallString = symbol.ToString();
            if (symbol.ReturnsVoid)
                methodCallString = "void " + methodCallString;
            else
                methodCallString = symbol.ReturnType.ToString() + " " + methodCallString;
            return methodCallString;
        }

        // return 2 if the project targets windows phone 8 os, return 1 if targetting windows phone 7,7.1.
        public static int IsWindowsPhoneProject(this Project project)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(project.FilePath);

            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            var node = doc.SelectSingleNode("//x:TargetFrameworkIdentifier", mgr);
            if (node != null && node.InnerText.ToString().Equals("WindowsPhone"))
                return 2;

            var profileNode = doc.SelectSingleNode("//x:TargetFrameworkProfile", mgr);
            if (profileNode != null && profileNode.InnerText.ToString().Contains("WindowsPhone"))
                return 1;

            var node2 = doc.SelectSingleNode("//x:XnaPlatform", mgr);
            if (node2 != null && node2.InnerText.ToString().Equals("Windows Phone"))
                return 1;


            return 0;
        }

        public static bool IsWPProject(this Project project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone") || a.Display.Contains("WindowsPhone"));
        }

        public static bool IsWP8Project(this Project project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone\\v8"));
        }

        public static bool IsNet40Project(this Project project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.0"));
        }

        public static bool IsNet45Project(this Project project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.5") || a.Display.Contains(".NETCore\\v4.5"));
        }

        // (1) MAIN PATTERNS: TAP, EAP, APM
        public static bool IsTAPMethod(this MethodSymbol symbol)
        {
            return symbol.ReturnTask() && !symbol.ToString().StartsWith("System.Threading.Tasks");
        }

        public static bool IsEAPMethod(this InvocationExpressionSyntax invocation)
        {
            return invocation.Expression.ToString().ToLower().EndsWith("async") &&
                   invocation.Ancestors().OfType<MethodDeclarationSyntax>().Any(node =>
                                                                           node.DescendantNodes()
                                                                           .OfType<BinaryExpressionSyntax>()
                                                                           .Any(a => a.Left.ToString().ToLower().EndsWith("completed")));
        }

        public static bool IsAPMBeginMethod(this MethodSymbol symbol)
        {
            return !IsAsyncDelegate(symbol) && symbol.Parameters.ToString().Contains("AsyncCallback") && !(symbol.ReturnsVoid) && symbol.ReturnType.ToString().Contains("IAsyncResult");
        }

        // (2) WAYS OF OFFLOADING THE WORK TO ANOTHER THREAD: TPL, THREADING, THREADPOOL, ACTION/FUNC.BEGININVOKE,  BACKGROUNDWORKER
        public static bool IsTPLMethod(this MethodSymbol symbol)
        {
            return symbol.ReturnTask() && symbol.ToString().StartsWith("System.Threading.Tasks");
        }

        public static bool IsThreadPoolQueueUserWorkItem(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("ThreadPool.QueueUserWorkItem");
        }

        public static bool IsBackgroundWorkerMethod(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("BackgroundWorker.RunWorkerAsync");
        }

        public static bool IsThreadStart(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Thread.Start");
        }

        public static bool IsAsyncDelegate(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Invoke") &&
                !(symbol.ReturnsVoid) && symbol.ReturnType.ToString().Contains("IAsyncResult");
        }

        // (3) WAYS OF UPDATING GUI: CONTROL.BEGININVOKE, DISPATCHER.BEGININVOKE, ISYNCHRONIZE.BEGININVOKE

        public static bool IsISynchronizeInvokeMethod(this MethodSymbol symbol)
        {
            return symbol.ToString().StartsWith("System.ComponentModel.ISynchronizeInvoke");
        }

        public static bool IsControlBeginInvoke(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Control.BeginInvoke");
        }

        public static bool IsDispatcherBeginInvoke(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Dispatcher.BeginInvoke");
        }

        public static bool IsDispatcherInvoke(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Dispatcher.Invoke");
        }

        // END

        public static bool IsAPMEndMethod(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("IAsyncResult") && symbol.Name.StartsWith("End");
        }

        public static bool ReturnTask(this MethodSymbol symbol)
        {
            return !symbol.ReturnsVoid && symbol.ReturnType.ToString().Contains("System.Threading.Tasks.Task");
        }

        public static bool IsInSystemWindows(this UsingDirectiveSyntax node)
        {
            return node.Name.ToString().StartsWith("System.Windows");
        }

        public static bool HasEventArgsParameter(this MethodDeclarationSyntax method)
        {
            return method.ParameterList.Parameters.Any(param => param.Type.ToString().EndsWith("EventArgs"));
        }


        public static bool HasAsyncModifier(this MethodDeclarationSyntax method)
        {
            return method.Modifiers.ToString().Contains("async");
        }

        public static MethodDeclarationSyntax FindMethodDeclarationNode(this MethodSymbol methodCallSymbol)
        {
            if (methodCallSymbol == null)
                return null;

            var nodes = methodCallSymbol.DeclaringSyntaxReferences.Select(a => a.GetSyntax());

            if (nodes == null || nodes.Count() == 0)
                return null;

            var methodDeclarationNodes = nodes.OfType<MethodDeclarationSyntax>();

            if (methodDeclarationNodes.Count() != 0)
                return methodDeclarationNodes.First();

            return null;

            // above one is not always working. basically, above one is the shortcut for the below one!

            //var def = methodCallSymbol.FindSourceDefinition(currentSolution);

            //if (def != null && def.Locations != null && def.Locations.Count > 0)
            //{
            //    //methodCallSymbol.DeclaringSyntaxNodes.Firs
            //    var loc = def.Locations.First();

            //    Solution s;
            //    s.
            //    var node = loc.SourceTree.GetRoot().FindToken(loc.SourceSpan.Start).Parent;
            //    if (node is MethodDeclarationSyntax)
            //        return (MethodDeclarationSyntax)node;
            //}
        }

        public static int CompilationErrorCount(this Solution solution)
        {
            return solution
                .GetDiagnostics()
                .Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        public static IEnumerable<Diagnostic> GetDiagnostics(this Solution solution)
        {
            if (solution == null) throw new ArgumentNullException("solution");

            return solution.Projects
                .Select(project => project.GetCompilationAsync().Result)
                .SelectMany(compilation => compilation.GetDiagnostics());
        }

        public static async Task<Solution> TryLoadSolutionAsync(this MSBuildWorkspace workspace, string solutionPath)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (solutionPath == null) throw new ArgumentNullException("solutionPath");

            Logger.Trace("Trying to load solution file: {0}", solutionPath);

            try
            {
                return await workspace.OpenSolutionAsync(solutionPath);
            }
            catch (Exception ex)
            {
                Logger.Warn("Solution not analyzed: {0}: Reason: {1}", solutionPath, ex.Message);

                return null;
            }
        }
    }
}
