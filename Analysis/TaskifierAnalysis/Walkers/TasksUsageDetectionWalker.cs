using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utilities;

namespace Analysis
{
    class TasksUsageDetectionWalker : CSharpSyntaxWalker
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
                Enums.TasksNamespaceDetected type = CheckTasksUsage(symbol);
                Result.StoreDetectedTasksNamespaceUsage(type);
                Result.WriteDetectedTasksNamespaceUsage(type, Document.FilePath, symbol, node);

            }

            base.VisitObjectCreationExpression(node);
        }

        private Enums.TasksNamespaceDetected CheckTasksUsage(ISymbol symbol)
        {
            Enums.TasksNamespaceDetected type = Enums.TasksNamespaceDetected.None;

            if (symbol.ContainingNamespace.ToString().Equals("System.Threading.Tasks"))
            {
                var className = (symbol.ContainingType.ToString());


                if (className.Equals("System.Threading.Tasks.Task") || className.StartsWith("System.Threading.Tasks.Task<"))
                {
                    type = Enums.TasksNamespaceDetected.TaskClass;
                }
                else if (className.Equals("System.Threading.Tasks.Parallel"))
                {
                    type = Enums.TasksNamespaceDetected.ParallelClass;
                }
                else
                {
                    type = Enums.TasksNamespaceDetected.OtherClass;
                }

            }

            return type;
        }




        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                Enums.TasksNamespaceDetected type = CheckTasksUsage(symbol);
                Result.StoreDetectedTasksNamespaceUsage(type);
                Result.WriteDetectedTasksNamespaceUsage(type, Document.FilePath, symbol, node);
            }


            base.VisitInvocationExpression(node);
        }

        

    }
}
