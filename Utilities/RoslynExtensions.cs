using System;
using System.Linq;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.Xml;
using System.Diagnostics;
using System.IO;

namespace Analysis
{
    /// <summary>
    /// Several handy extension methods for Roslyn types.
    /// </summary>
    public static class RoslynExtensions
    {

        public static bool IsCSProject(this IProject project)
        {
            return project.LanguageServices.Language.Equals("C#");
        }

        // return 2 if the project targets windows phone 8 os, return 1 if targetting windows phone 7,7.1. 
        public static int IsWindowsPhoneProject(this IProject project)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(project.FilePath);

            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            var node = doc.SelectSingleNode("//x:TargetFrameworkIdentifier", mgr);
            if (node != null)
            {
                if (node.InnerText.ToString().Equals("WindowsPhone"))
                    return 2;
                else if (node.InnerText.ToString().Equals("Silverlight"))
                {
                    var profileNode = doc.SelectSingleNode("//x:TargetFrameworkProfile", mgr);
                    if (profileNode != null && profileNode.InnerText.ToString().Contains("WindowsPhone"))
                        return 1;
                }
            }
            return 0;
        }

        public static bool IsWPProject(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone") || a.Display.Contains("WindowsPhone"));
        }

        public static bool IsWP8Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone\\v8"));
        }

        public static bool IsNet40Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.0"));
        }

        public static bool IsNet45Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.5") || a.Display.Contains(".NETCore\\v4.5"));
        }


        // (1) MAIN PATTERNS: TAP, EAP, APM
        public static bool IsTAPMethod(this MethodSymbol symbol)
        {
            return !symbol.ReturnsVoid && symbol.ReturnType.ToString().Contains("System.Threading.Tasks.Task") 
                                       && symbol.ToString().ToLower().Contains("async(");
        }

        public static bool IsEAPMethod(this InvocationExpressionSyntax invocation)
        {
            return invocation.Expression.ToString().ToLower().EndsWith("async") && 
                   invocation.Ancestors().OfType<MethodDeclarationSyntax>().First()
                                                                           .DescendantNodes()
                                                                           .OfType<BinaryExpressionSyntax>()
                                                                           .Any(a => a.Left.ToString().ToLower().EndsWith("completed"));
        }
        public static bool IsAPMBeginMethod(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("System.AsyncCallback") && (!symbol.ReturnsVoid && symbol.ReturnType.ToString().Contains("System.IAsyncResult"));
        }



        // (2) WAYS OF OFFLOADING THE WORK TO ANOTHER THREAD: TPL, THREADING, THREADPOOL, ACTION/FUNC.BEGININVOKE,  BACKGROUNDWORKER
        public static bool IsTPLMethod(this MethodSymbol symbol)
        {
            return symbol.ToString().StartsWith("System.Threading.Tasks");
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
            return (symbol.ToString().Contains("System.Func") || symbol.ToString().Contains("System.Action")) && symbol.ToString().Contains("BeginInvoke") ;
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

            var nodes = methodCallSymbol.DeclaringSyntaxNodes;

            if (nodes == null || nodes.Count == 0)
                return null;

            if (nodes.First() is MethodDeclarationSyntax)
                return (MethodDeclarationSyntax)nodes.First();

            return null;

            // above one is not always working. basically, above one is the shortcut for the below one!
 
            //var def = methodCallSymbol.FindSourceDefinition(currentSolution);

            //if (def != null && def.Locations != null && def.Locations.Count > 0)
            //{
            //    //methodCallSymbol.DeclaringSyntaxNodes.Firs
            //    var loc = def.Locations.First();
            //    var node = loc.SourceTree.GetRoot().FindToken(loc.SourceSpan.Start).Parent;
            //    if (node is MethodDeclarationSyntax)
            //        return (MethodDeclarationSyntax)node; 
            //}
        }
    }
}
