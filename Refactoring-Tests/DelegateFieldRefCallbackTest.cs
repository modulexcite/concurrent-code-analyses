using NUnit.Framework;
using System;

namespace Refactoring_Tests
{
    [TestFixture]
    public class DelegateFieldRefCallbackTest : APMToAsyncAwaitRefactoringTestBase
    {
        // [Test] TODO Delegates are only sometimes refactorable
        public void TestThatDelegateCallbackIsRefactoredCorrectly()
        {
            try
            {
                AssertThatOriginalCodeIsRefactoredCorrectly(
                    OriginalCode,
                    RefactoredCode,
                    FirstBeginInvocationFinder("request.BeginGetResponse")
                );

                Assert.Fail("Should have failed.");
            }
            catch (InvalidCastException)
            {
            }
        }

        private const string OriginalCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        protected AsyncCallback Callback;

        public void FireAndForgetDelegate()
        {
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            request.BeginGetResponse(Callback, request);

            DoSomethingWhileGetResponseIsRunning();
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";

        private const string RefactoredCode = @"";
    }
}
