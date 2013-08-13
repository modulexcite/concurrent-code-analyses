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
                DoSomethingWithResponse(response);
            }, null);

            DoSomethingWhileGetResponseIsRunning();
        }

        private static WebResponse Nested0(IAsyncResult result, WebRequest request)
        {
            return Nested1(result, request);
        }

        private static WebResponse Nested1(IAsyncResult result, WebRequest request)
        {
            return request.EndGetResponse(result);
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
            var response = Nested0(task, request).GetAwaiter().GetResult();
            DoSomethingWithResponse(response);
        }

        private static async Task<WebResponse> Nested0(Task<WebResponse> task, WebRequest request)
        {
            return Nested1(task, request).GetAwaiter().GetResult();
        }

        private static async Task<WebResponse> Nested1(Task<WebResponse> task, WebRequest request)
        {
            return task.GetAwaiter().GetResult();
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
