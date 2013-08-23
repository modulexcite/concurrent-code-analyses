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

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCodeWithUsedAsyncState, RefactoredCodeWithUsedAsyncState, statementFinder);
        }

        // [Test] TODO: Enable this test if AsyncState is removed when it is unused.
        public void TestThatPassedAsyncStateIsRemovedWhenUnused()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<InvocationExpressionSyntax>()
                                .First(invocation => invocation.ToString().Contains("request.BeginGetResponse"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCodeWithUnusedAsyncState, RefactoredCodeWithIgnoredAsyncState, statementFinder);
        }

        private const string OriginalCodeWithUsedAsyncState = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(Callback, request);
        }

        private void Callback(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            var response = request.EndGetResponse(result);

            DoSomethingWithRequestAndResponse(request, response);
        }

        private static void DoSomethingWithRequestAndResponse(WebRequest request, WebResponse response) { }
    }
}";

        private const string RefactoredCodeWithUsedAsyncState = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public async void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            var task = request.GetResponseAsync();
            Callback(task, request).GetAwaiter().GetResult();
        }

        private async Task Callback(Task<WebResponse> task, WebRequest request)
        {
            var response = task.GetAwaiter().GetResult();

            DoSomethingWithRequestAndResponse(request, response);
        }

        private static void DoSomethingWithRequestAndResponse(WebRequest request, WebResponse response) { }
    }
}";

        private const string OriginalCodeWithUnusedAsyncState = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(Callback, request);
        }

        private void Callback(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            var response = request.EndGetResponse(result);
        }
    }
}";

        private const string RefactoredCodeWithIgnoredAsyncState = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public async void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            var task = request.GetResponseAsync();
            Callback(task).GetAwaiter().GetResult();
        }

        private async Task Callback(Task<WebResponse> task)
        {
            var response = task.GetAwaiter().GetResult();
        }
    }
}";
    }
}
