using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System.Linq;

namespace Refactoring_Tests
{
    [TestFixture]
    public class TryCatchBlockTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatEndXxxInTryBlockIsRefactoredCorrectly()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<InvocationExpressionSyntax>()
                                .First(node => node.ToString().Contains("request.BeginGetResponse"));

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

            try
            {
                var response = request.EndGetResponse(result);

                DoSomethingWithResponse(response);
            }
            catch (WebException e)
            {
                HandleException(e);
            }
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
        private static void HandleException(WebException e) { }
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
            Callback(task).GetAwaiter().GetResult();
        }

        private async Task Callback(Task<WebResponse> task)
        {

            try
            {
                var response = task.GetAwaiter().GetResult();

                DoSomethingWithResponse(response);
            }
            catch (WebException e)
            {
                HandleException(e);
            }
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
        private static void HandleException(WebException e) { }
    }
}";
    }
}
