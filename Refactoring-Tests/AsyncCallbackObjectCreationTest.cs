using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace Refactoring_Tests
{
    [TestFixture]
    public class AsyncCallbackObjectCreationTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatAsyncCallbackObjectCreationWithLambdaExpressionIsRefactoredCorrectly()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<InvocationExpressionSyntax>()
                                .First(invocation => invocation.ToString().Contains("request.BeginGetResponse"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCodeWithLambdaExpression, RefactoredCodeWithLambdaExpression, statementFinder);
        }

        [Test]
        public void TestThatAsyncCallbackObjectCreationWithMethodReferenceIsRefactoredCorrectly()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<InvocationExpressionSyntax>()
                                .First(invocation => invocation.ToString().Contains("request.BeginGetResponse"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCodeWithMethodReference, RefactoredCodeWithMethodReference, statementFinder);
        }

        private const string OriginalCodeWithLambdaExpression = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(new AsyncCallback(result => {
                var response = request.EndGetResponse(result);

                DoSomethingWithResponse(response);
            }), null);

            DoSomethingWhileGetResponseIsRunning();
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";

        private const string RefactoredCodeWithLambdaExpression = @"using System;
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
            var response = await task.ConfigureAwait(false);

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";

        private const string OriginalCodeWithMethodReference = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(new AsyncCallback(Callback), null);

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

        private const string RefactoredCodeWithMethodReference = @"using System;
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
            await Callback(task).ConfigureAwait(false);
        }

        private async Task Callback(Task<WebResponse> task)
        {
            var response = await task.ConfigureAwait(false);

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
