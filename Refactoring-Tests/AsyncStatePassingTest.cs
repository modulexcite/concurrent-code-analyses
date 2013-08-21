using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace Refactoring_Tests
{
    [TestFixture]
    public class AsyncStatePassingTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatPassedAsyncStateIsIntroducedAsParameterForCallbacks()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<InvocationExpressionSyntax>()
                                .First(invocation => invocation.ToString().Contains("request.BeginGetResponse"));

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
            request.BeginGetResponse(Callback, request);

            DoSomethingWhileGetResponseIsRunning();
        }

        private void Callback(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            var response = request.EndGetResponse(result);

            DoSomethingWithResponse(request, response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithRequestAndResponse(WebRequest request, WebResponse response) { }
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

            DoSomethingWithRequestAndResponse(request, response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithRequestAndResponse(WebRequest request, WebResponse response) { }
    }
}";
    }
}
