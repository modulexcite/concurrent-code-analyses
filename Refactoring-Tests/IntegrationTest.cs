using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Refactoring;

namespace Refactoring_Tests
{
    [TestFixture]
    public class IntegrationTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void Test()
        {
            //var workspace = new CustomWorkspace();
            var workspace = MSBuildWorkspace.Create();
            //var projectId = workspace.AddProject("ProjectUnderTest", LanguageNames.CSharp);
            //var documentId = workspace.AddDocument(projectId, "SourceFileUnderTest.cs", OriginalCode);

            DocumentId documentId = null;

            var originalSolution = workspace.CurrentSolution;
            var originalDocument = originalSolution.GetDocument(documentId);
            var originalSyntaxTree = (SyntaxTree)originalDocument.GetSyntaxTreeAsync().Result;

            StatementFinder statementFinder =
                tree => tree.GetRoot()
                            .DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .First(node => node.Expression.ToString().Equals("request.BeginGetResponse"));

            var originalInvocation = statementFinder(originalSyntaxTree);
            var annotatedInvocation = originalInvocation.WithAdditionalAnnotations(new RefactorableAPMInstance());

            var annotatedSyntax = originalSyntaxTree.GetRoot().ReplaceNode(originalInvocation, annotatedInvocation);
            var annotatedSyntaxTree = SyntaxTree.Create(annotatedSyntax);
            var annotatedDocument = originalDocument.WithSyntaxRoot(annotatedSyntax);
            var annotatedSemanticModel = annotatedDocument.GetSemanticModelAsync().Result;

            var rewrittenSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(annotatedDocument, workspace);
            //var rewrittenDocument = originalDocument.WithSyntaxRoot(rewrittenSyntax);

            var rewrittenSolution = originalSolution.WithDocumentSyntaxRoot(documentId, rewrittenSyntax);

            if (!workspace.TryApplyChanges(rewrittenSolution))
            {
                throw new Exception("Failed to apply changes in rewritten solution to workspace");
            }
        }

        private const string OriginalCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(Callback, request);

            DoSomethingWhileGetResponseIsRunning();
        }

        private void Callback(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            var response = request.EndGetResponse(result);

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";

        private const string RefactoredCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public async void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            var task = request.GetResponseAsync();

            DoSomethingWhileGetResponseIsRunning();
            Callback(task, request).GetAwaiter().GetResult();
        }

        private async Task Callback(Task<WebResponse> task, WebRequest request)
        {
            var response = task.GetAwaiter().GetResult();

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
