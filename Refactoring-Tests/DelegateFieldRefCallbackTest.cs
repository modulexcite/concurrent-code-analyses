﻿using NUnit.Framework;
using System;
using Refactoring;

namespace Refactoring_Tests
{
    [TestFixture]
    public class DelegateFieldRefCallbackTest : APMToAsyncAwaitRefactoringTestBase
    {
        [Test]
        public void TestThatDelegateCallbackIsRefactoredCorrectly()
        {
            AssertThatRefactoringOriginalCodeThrowsPreconditionException(
                OriginalCode,
                FirstBeginInvocationFinder("request.BeginGetResponse")
            );
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
    }
}
