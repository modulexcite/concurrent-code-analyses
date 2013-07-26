using System.Linq;
using NUnit.Framework;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class LambdaCallbackTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatTheSimpleCaseWithLambdaCallbackIsRefactoredCorrectly()
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
            request.BeginGetResponse((result) => {
                var response = request.EndGetResponse(result);

                DoSomethingWithResponse(response);
            }, null);

            DoSomethingWhileGetResponseIsRunning();
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
            var task = request.GetResponseAsync();

            DoSomethingWhileGetResponseIsRunning();

            var response = await task.ConfigureAwait(false);
            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
