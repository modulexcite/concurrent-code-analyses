using System.Linq;
using NUnit.Framework;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class EndXxxDeeperInCallGraphTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatNestedEndGetResponseIsRefactoredCorrectly()
        {
            StatementFinder statementFinder =
                syntax => syntax.DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First();

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCode, statementFinder);
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
            request.BeginGetResponse(result =>
            {
                var response = Nested0(result, request);
                DoSomethingWithResult(response);
            }, null);

            DoSomethingWhileWebRequestIsRunning();
        }

        private static WebResponse Nested0(IAsyncResult result, WebRequest request)
        {
            return Nested1(result, request);
        }

        private static WebResponse Nested1(IAsyncResult result, WebRequest request)
        {
            return request.EndGetResponse(result);
        }
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
            var request = WebRequest.Create(""http://www.google.com/"");
            var task = request.GetResponseAsync(result => Callback(result, request), null);

            DoSomethingWhileGetResponseIsRunning();

            Callback(task, request).GetAwaiter().GetResult();
        }

        private async Task Callback(Task<WebResponse> task, WebRequest, request)
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
