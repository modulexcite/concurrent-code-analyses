using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;

namespace Analysis
{
    internal class ThreadUsageDetectionWalker : CSharpSyntaxWalker
    {
        public TaskifierAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public bool IsEventHandlerWalkerEnabled { get; set; }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if ((node.BaseList != null) && (node.BaseList.ToString().Contains("ClientBase") || node.BaseList.ToString().Contains("ChannelBase")))
            {
                // IGNORE WCF SERVICES WHICH ARE GENERATED AUTOMATICALLY
            }
            else
                base.VisitClassDeclaration(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var symbol = SemanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null)
            {
                Enums.ThreadingNamespaceDetected type = CheckThreadingUsage(symbol);
                Result.StoreDetectedThreadingNamespaceUsage(type);
                Result.WriteDetectedThreadingNamespaceUsage(type, Document.FilePath, symbol, node);
            }

            base.VisitObjectCreationExpression(node);
        }

        private Enums.ThreadingNamespaceDetected CheckThreadingUsage(ISymbol symbol)
        {
            Enums.ThreadingNamespaceDetected type = Enums.ThreadingNamespaceDetected.None;

            if (symbol.ContainingNamespace.ToString().Equals("System.Threading"))
            {
                var className = (symbol.ContainingType.ToString());

               
                if (className.Equals("System.Threading.Thread"))
                {
                    type = Enums.ThreadingNamespaceDetected.ThreadClass;
                }
                else if (className.Equals("System.Threading.ThreadPool"))
                {
                    type = Enums.ThreadingNamespaceDetected.ThreadpoolClass;
                }
                else
                {
                    type = Enums.ThreadingNamespaceDetected.OtherClass;
                }

            }

            return type;
        }




        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;
            
            if (symbol != null)
            {

                Enums.ThreadingNamespaceDetected type = CheckThreadingUsage(symbol);
                Result.StoreDetectedThreadingNamespaceUsage(type);
                Result.WriteDetectedThreadingNamespaceUsage(type, Document.FilePath, symbol, node);
                



                //Result.StoreDetectedAsyncUsage(asynctype);
                //Result.WriteDetectedAsyncUsage(asynctype, Document.FilePath, symbol);
                //Result.WriteDetectedAsyncUsageToTable(asynctype,Document,symbol,node);
                //if (asynctype == Enums.AsyncDetected.Task || asynctype == Enums.AsyncDetected.Threadpool || asynctype == Enums.AsyncDetected.Thread)
                //{
                //    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                //    {

                //        var methodCallSymbol = (MethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;

                //        if (methodCallSymbol != null)
                //        {
                //            var synctype = ((MethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

                //            if (synctype != Utilities.Enums.SyncDetected.None)
                //                Logs.TempLog.Info("LONGRUNNING {0} {1}\r\n{2}\r\n--------------------------", methodCallSymbol, Document.FilePath, node);
                //        }
                //    }
                //}
                 
            }


            base.VisitInvocationExpression(node);
        }

        

    }
}