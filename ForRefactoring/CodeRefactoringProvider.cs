using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using Utilities;
using System.Diagnostics;

namespace ForRefactoring
{

    //[ExportCodeRefactoringProvider("RefactoringReadability", LanguageNames.CSharp )]
    class CodeRefactoringProvider : ICodeRefactoringProvider
    {
        public CodeRefactoring GetRefactoring(IDocument document, TextSpan textSpan, CancellationToken cancellationToken)
        {

            var root = (SyntaxNode)document.GetSyntaxRoot(cancellationToken);
            var loops = root.DescendantNodes(textSpan).OfType<ForStatementSyntax>();

            if (loops.Count() == 0)
                return null;

            var loop = loops.First();

            if (AnalyzeForLoop(loop))
                return new CodeRefactoring( 
                    new[] { new CodeAction(document, loop) },
                       loop.Span );
               
            return null;
        }


        private bool AnalyzeForLoop(ForStatementSyntax loop)
        {
            var n = Helper.getNextNode(loop);
            Debug.WriteLine("***" + n.ToString());
            if (!n.ToString().Contains("Task.WaitAll"))
                return false;

            var statements = (loop.Statement as BlockSyntax).Statements;
            //var st = statements.First() as LocalDeclarationStatementSyntax;

            //var itValue = st.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();

            StatementSyntax createTaskStatement = statements.Where(a => a.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Any()).First();

            var tree = loop.SyntaxTree;
            MetadataReference mscorlib = MetadataReference.CreateAssemblyReference(
                                 "mscorlib");
            Compilation compilation = Compilation.Create("HelloWorld")
                .AddReferences(mscorlib)
                .AddSyntaxTrees(tree);

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            DataFlowAnalysis dataFlowStatement = semanticModel.AnalyzeDataFlow(createTaskStatement);
            DataFlowAnalysis dataFlowLoop = semanticModel.AnalyzeDataFlow(loop);

            var vars = dataFlowStatement.DataFlowsIn.Except(dataFlowLoop.DataFlowsIn); 
            

            

            foreach (var t in vars)
            {

                var k = t.DeclaringSyntaxNodes.First();
                //var k2 = statements.Where(st => st.DescendantNodes().Contains(k)).First();
                Debug.WriteLine("flowsin: " + t + " declaring " + k );
            }

            

            return true;


        }
    }
}
