using System;
using System.Linq;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

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

        public static bool IsWPProject(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone") || a.Display.Contains("WindowsPhone"));
        }

        public static bool IsWP8Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone\\v8"));
        }

        public static bool IsAzureProject(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Azure"));
        }

        public static bool IsNet40Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.0"));
        }

        public static bool IsNet45Project(this IProject project)
        {
            return project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.5") || a.Display.Contains(".NETCore\\v4.5"));
        }

        /// <summary>
        /// Test whether method is Control.BeginInvoke(...), a kind of APM public IAsyncResult BeginInvoke(Delegate method).
        /// </summary>
        /// <param name="symbol">Method to test</param>
        /// <returns>true if it is an instance of Control.BeginInvoke(...), otherwise false.</returns>
        public static bool IsControlBeginInvoke(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Control.BeginInvoke");
        }

        /// <summary>
        /// Test for DispatcherOperation BeginInvoke(Delegate method,params Object[] args).
        /// </summary>
        /// <param name="symbol">Method to test</param>
        /// <returns>true if it is an instance of Dispatcher.BeginInvoke(...), otherwise false.</returns>
        public static bool IsDispatcherBeginInvoke(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Dispatcher.BeginInvoke");
        }

        /// <summary>
        /// Test for ThreadPool.QueueUserWorkItem(...).
        /// </summary>
        /// <param name="symbol">Method to test</param>
        /// <returns>true if 'symbol' is an instance of ThreadPool.QueueUserWorkItem(...), otherwise false.</returns>
        public static bool IsThreadPoolQueueUserWorkItem(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("ThreadPool.QueueUserWorkItem");
        }

        public static bool ContainsBeginInvoke(this InvocationExpressionSyntax invocation)
        {
            return invocation.ToString().Contains("BeginInvoke");
        }

        public static bool ContainsSynchronizationContext(this InvocationExpressionSyntax invocation)
        {
            return invocation.ToString().Contains("SynchronizationContext");
        }

        public static bool IsBackgroundWorkerRunWorkerAsync(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("BackgroundWorker.RunWorkerAsync");
        }

        public static bool IsThreadStart(this MethodSymbol symbol)
        {
            return symbol.ToString().Contains("Thread.Start");
        }

        public static bool HasNonVoidReturnType(this MethodSymbol symbol)
        {
            return symbol.ReturnsVoid;
        }

        public static bool ReturnsTask(this MethodSymbol symbol)
        {
            return symbol.HasNonVoidReturnType() && symbol.ReturnType.ToString().Contains("System.Threading.Tasks.Task");
        }

        public static bool CallsAsyncMethod(this InvocationExpressionSyntax invocation)
        {
            return invocation.Expression.ToString().ToLower().EndsWith("async");
        }

        public static Boolean IsEAPCompletedMethod(this MethodDeclarationSyntax methodDeclaration)
        {
            // TODO: Shouldn't this be EndsWith instead of Contains?
            return methodDeclaration.ToString().Contains("Completed");
        }

        public static bool IsInSystemWindows(this UsingDirectiveSyntax node)
        {
            return node.Name.ToString().StartsWith("System.Windows");
        }

        public static bool ReturnsIAsyncResult(this MethodSymbol symbol)
        {
            return !symbol.ReturnsVoid && symbol.ReturnType.ToString().Contains("System.IAsyncResult");
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
        }
    }
}
