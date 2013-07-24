using System.Linq;
using NUnit.Framework;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class SimpleTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatTheSimpleCaseIsRefactoredCorrectly()
        {
            StatementFinder actualStatementFinder =
                syntax => syntax.DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(invocation => invocation.ToString().Contains("Begin"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCode, actualStatementFinder);
        }

        private const string OriginalCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.google.com/"");
            request.BeginGetResponse(CallBack, request);

            DoSomethingWhileGetResponseIsRunning();
        }

        private void CallBack(IAsyncResult result)
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
            var request = WebRequest.Create(""http://www.google.com/"");
            var task = request.GetResponseAsync().ConfigureAwait(false);

            DoSomethingWhileGetResponseIsRunning();

            var response = await task;
            CallBack(response, request);
           
        }

        private void CallBack(WebResponse response, WebRequest request)
        {
            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
