﻿using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace Refactoring_Tests
{
    [TestFixture]
    public class UnusedIAsyncResultCapturedAtBeginXxxTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatUnusuedIAsyncResultCapturedAtBeginXxxIsIgnored()
        {
            StatementFinder statementFinder =
                syntax => syntax.GetRoot().DescendantNodes()
                                .OfType<ExpressionStatementSyntax>()
                                .First(invocation => invocation.ToString().Contains("Begin"));

            AssertThatOriginalCodeIsRefactoredCorrectly(OriginalCode, RefactoredCode, statementFinder);
        }

        // TODO [Test]
        public void TestThatUsedIAsyncResultCapturedAtBeginXxxFailsPrecondition()
        {
            // TODO
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
            var result = request.BeginGetResponse(result => {
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
            var request = WebRequest.Create(""http://www.microsoft.com/"");
            var task = request.GetResponseAsync();

            DoSomethingWhileGetResponseIsRunning();
            var response = task.GetAwaiter().GetResult();

            DoSomethingWithResponse(response);
        }

        private static void DoSomethingWhileGetResponseIsRunning() { }
        private static void DoSomethingWithResponse(WebResponse response) { }
    }
}";
    }
}
