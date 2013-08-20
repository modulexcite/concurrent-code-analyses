using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System.Linq;

namespace Refactoring_Tests
{
    [TestFixture]
    public class MethodRefCallbackTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatTheSimpleCaseWithMethodRefCallbackIsRefactoredCorrectly()
        {
            StatementFinder actualStatementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(invocation => invocation.ToString().Contains("Begin"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCode, actualStatementFinder);
        }

        [Test]
        public void TestThatSimpleLambdaWithoutBlockIsRefactoredCorrectly()
        {
            StatementFinder actualStatementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(invocation => invocation.ToString().Contains("Begin"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCodeWithSimpleLambdaWithoutBlock, RefactoredCode, actualStatementFinder);
        }

        [Test]
        public void TestThatParenthesizedLambdaWithoutBlockIsRefactoredCorrectly()
        {
            StatementFinder actualStatementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(invocation => invocation.ToString().Contains("Begin"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCodeWithParenthesizedLambdaWithoutBlock, RefactoredCode, actualStatementFinder);
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

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";

        private const string OriginalCodeWithSimpleLambdaWithoutBlock = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(result => Callback(result), request);

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

        private const string OriginalCodeWithParenthesizedLambdaWithoutBlock = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse((result) => Callback(result), request);

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
            var response = task.GetAwaiter().GetResult();

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
