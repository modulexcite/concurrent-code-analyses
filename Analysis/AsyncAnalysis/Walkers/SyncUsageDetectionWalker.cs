using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Utilities;

namespace Analysis
{
    internal class SyncUsageDetectionWalker : CSharpSyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                var synctype = DetectSynchronousUsages((IMethodSymbol)symbol.OriginalDefinition);
                Result.StoreDetectedSyncUsage(synctype);
                Result.WriteDetectedSyncUsage(synctype, Document.FilePath, (IMethodSymbol)symbol.OriginalDefinition);
                if (synctype != Utilities.Enums.SyncDetected.None
                        && node.Ancestors().OfType<MethodDeclarationSyntax>().Any(method => method.HasAsyncModifier()))
                {
                    Result.syncUsageResults.NumGUIBlockingSyncUsages++;
                    Logs.TempLog.Info(@"GUIBLOCKING {0}", node.Ancestors().OfType<MethodDeclarationSyntax>().First().ToString());
                }
            }
        }

        public Enums.SyncDetected DetectSynchronousUsages(IMethodSymbol methodCallSymbol)
        {
            var list = SemanticModel.LookupSymbols(0, methodCallSymbol.ContainingType, includeReducedExtensionMethods:true);

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