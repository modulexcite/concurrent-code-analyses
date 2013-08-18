using System;
using System.Linq;
using NUnit.Framework;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class DelegateFieldRefCallbackTest : APMToAsyncAwaitRefactoringTestBase
    {
        [TestCase(Ignore = true, IgnoreReason = "Delegates are only sometimes refactorable")]
        public void TestThatDelegateCallbackIsRefactoredCorrectly()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(node => node.ToString().Contains("Begin"));

            try
            {
                AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCode, statementFinder);
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
