using Roslyn.Compilers.CSharp;

namespace Analysis
{
    internal class EventHandlerMethodsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis {get; set;}
        public AsyncAnalysisResult Result { get; set; }

        private bool uiClass;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !uiClass)
            {
                uiClass = true;
                Result.NumUIClasses++;
            }
            base.VisitUsingDirective(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasEventArgsParameter())
            {
                Result.NumEventHandlerMethods++;
                Analysis.ProcessMethodCallsInMethod(node, 0);
            }

            if (node.HasAsyncModifier())
            {
                if (node.ReturnType.ToString().Equals("void"))
                    Result.NumAsyncVoidMethods++;
                else
                    Result.NumAsyncTaskMethods++;
            }
            base.VisitMethodDeclaration(node);
        }
    }
}