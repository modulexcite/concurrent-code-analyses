using NUnit.Framework;

namespace Refactoring_Tests
{
    [TestFixture]
    public class APMBeginInLocalDeclarationStatementTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatAPMBeginRewritingUsesNonCollidingIdentifierForLambdaParameter()
        {
            AssertThatOriginalCodeIsRefactoredCorrectly(
                OriginalCodeWithUsedAsyncState,
                "",
                FirstBeginInvocationFinder("request.BeginGetResponse")
            );
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
            var result = request.BeginGetResponse(Callback, request);
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
    }
}
