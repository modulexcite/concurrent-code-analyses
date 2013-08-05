using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;

namespace Analysis
{
    internal class AsyncAwaitDetectionWalker : SyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public IDocument Document { get; set; }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasAsyncModifier())
            {
                if (node.ReturnType.ToString().Equals("void"))
                {
                    if (node.HasEventArgsParameter())
                        Result.asyncAwaitResults.NumAsyncVoidEventHandlerMethods++;
                    else
                        Result.asyncAwaitResults.NumAsyncVoidNonEventHandlerMethods++;
                }
                else
                    Result.asyncAwaitResults.NumAsyncTaskMethods++;

                if (!node.Body.ToString().Contains("await"))
                    Result.asyncAwaitResults.NumAsyncMethodsNotHavingAwait++;

                foreach (var invocationNode in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(invocationNode).Symbol;
                    if (symbol != null)
                    {
                        var synctype = DetectSynchronousUsages((MethodSymbol)symbol.OriginalDefinition);

                        if (synctype != Utilities.Enums.SyncDetected.None)
                        {
                            Logs.TempLog.Info("{0} {1}\r\n{2} {3} \r\n\r\n{4}\r\n --------------------------", synctype, Document.FilePath, symbol, invocationNode, node);
                            Logs.TempLog2.Info("{0} {1}", symbol.ContainingType, symbol, synctype);
                        }
                    }
                }

                int numAwaits = Regex.Matches(node.Body.ToString(), "await").Count;

                if (numAwaits > 3)
                    Logs.TempLog.Info("MANYAWAITS {0} \r\n------------------------------", node);

                //if (node.Body.ToString().Contains("ConfigureAwait"))
                //{
                //    Result.asyncAwaitResults.NumAsyncMethodsHavingConfigureAwait++;
                //    Logs.TempLog.Info(@"CONFIGUREAWAIT {0}", node.ToString());
                //}
                //if (Constants.BlockingMethodCalls.Any(a => node.Body.ToString().Contains(a)))
                //{
                //    Logs.TempLog.Info(@"BLOCKING {0}", node.ToString());
                //    Result.asyncAwaitResults.NumAsyncMethodsHavingBlockingCalls++;
                //}
            }

            base.VisitMethodDeclaration(node);
        }

        public Enums.SyncDetected DetectSynchronousUsages(MethodSymbol methodCallSymbol)
        {
            var list = SemanticModel.LookupSymbols(0, methodCallSymbol.ContainingType,
                                                    options: LookupOptions.IncludeExtensionMethods);

            var name = methodCallSymbol.Name;
            Enums.SyncDetected type = Enums.SyncDetected.None;

            if (name.Equals("Invoke"))
                return type;

            foreach (var tmp in list)
            {
                if (tmp.Name.Equals("Begin" + name))
                {
                    type |= Enums.SyncDetected.APMReplacable;
                }
                if (tmp.Name.Equals(name + "Async"))
                {
                    type |= Enums.SyncDetected.TAPReplacable;
                }
            }

            return type;
        }
    }
}