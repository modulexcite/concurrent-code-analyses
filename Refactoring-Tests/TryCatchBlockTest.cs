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

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCodeWithoutWhiteSpaceFix, statementFinder);
        }

        private const string OriginalCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForgetDelegate()
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

        private const string RefactoredCodeWithoutWhiteSpaceFix = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public async void FireAndForgetDelegate()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            var task = request.GetResponseAsync();

            DoSomethingWhileGetResponseIsRunning();
            var result = task.GetAwaiter().GetResult();
            Callback(request, result);
        }

        private void Callback(System.Net.WebRequest request, ? response)
        {

            try
            {

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
            var request = WebRequest.Create(""http://www.microsoft.com/"");
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
