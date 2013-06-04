using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using Utilities;
using System.Diagnostics;
namespace ForEachRefactoring
{
    class CodeAction : ICodeAction
    {
        private IDocument document;
        private ForEachStatementSyntax loop;

        
        public CodeAction(IDocument document, ForEachStatementSyntax loop)
        {
            this.document = document;
            this.loop = loop;
        }

        public string Description
        {
            get { return "Replace with Parallel.ForEach for readability"; }
        }

        public CodeActionEdit GetEdit(CancellationToken cancellationToken)
        {
            
            
            
            var function = loop.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().First().AddParameterListParameters(new[] { Syntax.Parameter(Syntax.Identifier(loop.Identifier.ToString())) });

            function = updateLambdaFunction(function);
   
            string arguments = string.Format("({0}, {1})", loop.Expression.ToString(), function.ToFullString());

            var newParallelFor= Syntax.ExpressionStatement( Syntax.InvocationExpression(
                    Syntax.ParseExpression("Parallel.ForEach"),
                    Syntax.ParseArgumentList(arguments)
               ));


            var leadingTrivia = loop.GetFirstToken().LeadingTrivia;
            var trailingTrivia = loop.GetLastToken().LeadingTrivia;

            var firstToken = newParallelFor.GetFirstToken();
            newParallelFor = newParallelFor.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(leadingTrivia));


            var oldRoot = (SyntaxNode)document.GetSyntaxRoot(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(loop, newParallelFor as SyntaxNode);
                
            newRoot= newRoot.RemoveNode(Helper.getNextNode(newRoot.DescendantNodes().OfType<ExpressionStatementSyntax>()
                .Where(a=> a.ToString().Contains("Parallel.For")).First()) as SyntaxNode, SyntaxRemoveOptions.KeepExteriorTrivia);

            newRoot = removeTaskRelatedMethods(newRoot);
            
            return new CodeActionEdit(document.UpdateSyntaxRoot(newRoot).Format());

            //// Create new token from old token, replace 'a' with 'z'
            //var newToken = Syntax.Identifier(
            //    token.LeadingTrivia,
            //    token.ToString().Replace('a', 'z'),
            //    token.TrailingTrivia);

            //var oldRoot = (SyntaxNode)document.GetSyntaxRoot(cancellationToken);
            //var newRoot = oldRoot.ReplaceToken(token, newToken);

            //return new CodeActionEdit(document.UpdateSyntaxRoot(newRoot));
        }

        private SyntaxNode removeTaskRelatedMethods(SyntaxNode node)
        {
            StatementSyntax createTaskStatement = (loop.Statement as BlockSyntax).Statements.Where(a => a.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Any()).First();


            string remove=null;
            if (createTaskStatement.ChildNodes().OfType<BinaryExpressionSyntax>().Any())
            {
                var left= createTaskStatement.ChildNodes().OfType<BinaryExpressionSyntax>().First().Left;

                if (left is ElementAccessExpressionSyntax)
                    remove= (left as ElementAccessExpressionSyntax).Expression.ToString();
                
            }

            if (remove == null)
            {
                var nextMethod = Helper.getNextNode(createTaskStatement);
                if (nextMethod != null && Helper.getNextNode(nextMethod)==null)
                {
                    remove = (nextMethod.ChildNodes().First().ChildNodes().First().ChildNodes().First() as IdentifierNameSyntax).Identifier.ToString();
                }
            }

            if (remove == null && Helper.getNextNode(createTaskStatement)!=null)
            {
                var nextMethod = Helper.getNextNode(Helper.getNextNode(createTaskStatement));
                if (nextMethod != null)
                {
                    remove = (nextMethod.ChildNodes().First().ChildNodes().First().ChildNodes().First() as IdentifierNameSyntax).Identifier.ToString();
                }
            }
           
           
            

            Debug.WriteLine("hell yeah"+remove);
            var removedStatement = node.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                .Where(a => a.DescendantNodes().OfType<VariableDeclaratorSyntax>().
                    Where(b => b.Identifier.ToString().Equals(remove))
                    .Any());
            if(removedStatement.Any())
                return node.RemoveNode(removedStatement.First(), SyntaxRemoveOptions.KeepExteriorTrivia);

            return node;
        }


        private ParenthesizedLambdaExpressionSyntax updateLambdaFunction(ParenthesizedLambdaExpressionSyntax function)
        {
            var tree = loop.SyntaxTree;
            MetadataReference mscorlib = MetadataReference.CreateAssemblyReference(
                                 "mscorlib");
            Compilation compilation = Compilation.Create("HelloWorld")
                .AddReferences(mscorlib)
                .AddSyntaxTrees(tree);

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            var oldFunctionBody= function.Body;

            
            List<StatementSyntax> newList = new List<StatementSyntax>();


            var statements = (loop.Statement as BlockSyntax).Statements;
            StatementSyntax createTaskStatement = statements.Where(a => a.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Any()).First();

            analyzeAndAdd(createTaskStatement, newList, semanticModel);

            if(oldFunctionBody is BlockSyntax)
                newList.Union((oldFunctionBody as BlockSyntax).Statements);
            else
                newList.Add(Syntax.ExpressionStatement((oldFunctionBody as InvocationExpressionSyntax)));

            removeIterationValue(newList);

            //newList = newList.Select(st => { var newSt= st.NormalizeWhitespace().ReplaceTrivia(new SyntaxTriviaList().Union(st.DescendantTrivia()), (_, __) => SyntaxTriviaList.Empty); return newSt;}).ToList();
            
            return function.ReplaceNode(oldFunctionBody, Syntax.Block(newList));
        
        }


        private void analyzeAndAdd(StatementSyntax st, List<StatementSyntax> list, SemanticModel semanticModel)
        {
            DataFlowAnalysis dataFlowStatement = semanticModel.AnalyzeDataFlow(st);
            DataFlowAnalysis dataFlowLoop = semanticModel.AnalyzeDataFlow(loop);


            var vars = dataFlowStatement.DataFlowsIn.Except(dataFlowLoop.DataFlowsIn);

            var statements = (loop.Statement as BlockSyntax).Statements;

            var tmpLst = st.DescendantNodes().OfType<ElementAccessExpressionSyntax>();

            foreach(var arrayAccess in tmpLst)
            {
                var isThereAny = statements.Where(a =>
                    {
                        if(a == st)
                            return false;

                        var assign = a.DescendantNodes().OfType<BinaryExpressionSyntax>();
                        
                        if (assign.Any() && assign.First().Left is ElementAccessExpressionSyntax)
                            return (assign.First().Left as ElementAccessExpressionSyntax).ToString().Equals(arrayAccess.ToString());
                        return false;
                    });
                if (isThereAny.Count() > 0)
                {
                    analyzeAndAdd(isThereAny.First(), list, semanticModel);
                    if(!list.Contains(isThereAny.First()))
                        list.Add(isThereAny.First());
                }

            }

            foreach (var t in vars)
            {

                var k = t.DeclaringSyntaxNodes.First();

                var tmp = statements.Where(stm => stm.DescendantNodes().Contains(k));

                if (tmp.Count() > 0)
                {
                    Debug.WriteLine("&&&&&&&&&&&&" + tmp.First());
                    analyzeAndAdd(tmp.First(), list, semanticModel);
                    if(!list.Contains(tmp.First()))
                        list.Add(tmp.First());
                }
            }

            
        }


        private void removeIterationValue(List<StatementSyntax> newList)
        {
            var id= loop.Identifier;

            SyntaxToken replace= loop.DescendantTokens().First();
            StatementSyntax removedSt= null;

            foreach(var a in newList.OfType<LocalDeclarationStatementSyntax>())
            {

                var t = a.DescendantNodes().OfType<IdentifierNameSyntax>().Where(b => b.ToString().Equals(id.ToString())).Any();
                if (t)
                {
                    removedSt = a;
                    replace = a.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier;
                }
            }

            newList.Remove(removedSt);

            for (int i = 0; i < newList.Count; i++)
            {
                StatementSyntax tmp= newList.ElementAt(i);
                
                newList.Remove(tmp);

                var oldTokens = tmp.DescendantTokens().Where(a=> a.ToString().Equals(replace.ToString()));

                tmp= tmp.ReplaceTokens(oldTokens, (oldToken, wth)=> id);


                newList.Insert(i, tmp);
            
            }

            
        }
    }
}
