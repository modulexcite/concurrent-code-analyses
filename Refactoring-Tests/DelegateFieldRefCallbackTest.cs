using System.Linq;
using NUnit.Framework;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class DelegateFieldRefCallbackTest : APMToAsyncAwaitRefactoringTestBase
    {
        [TestCase]
        public void TestThatDelegateCallbackIsRefactoredCorrectly()
        {
            StatementFinder statementFinder =
                syntax => syntax.DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(node => node.ToString().Contains("Begin"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCode, statementFinder);
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
            var request = WebRequest.Create(""http://www.google.com/"");
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
