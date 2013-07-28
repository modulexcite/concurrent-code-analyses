using System.Linq;
using NUnit.Framework;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class TryCatchBlockTest : APMToAsyncAwaitRefactoringTestBase
    {
        [TestCase]
        public void TestThatEndXxxInTryBlockIsRefactoredCorrectly()
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
        public void FireAndForgetDelegate()
        {
            var request = WebRequest.Create(""http://www.google.com/"");
            request.BeginGetResponse(Callback, request);

            DoSomethingWhileGetResponseIsRunning();
        }

        private void Callback(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;

            try
            {
                var response = result.EndGetResponse(result);

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
        public void FireAndForgetDelegate()
        {
            var request = WebRequest.Create(""http://www.google.com/"");
            var task = request.GetResponseAsync();

            DoSomethingWhileGetResponseIsRunning();

            Callback(task);
        }

        private void Callback(Task<WebResponse> task)
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
