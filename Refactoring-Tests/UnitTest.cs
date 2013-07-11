using NUnit.Framework;
using Refactoring;

namespace Refactoring_Tests
{
    [TestFixture]
    public class UnitTest
    {
        [Test]
        public void TestMethod()
        {
            var syntax = TestData.OriginalSyntaxTree.GetRoot().RefactorAPMToAsyncAwait(TestData.APMInvocation);

            Assert.That(syntax, Is.EqualTo(TestData.RefactoredSyntaxTree.GetRoot()));
            Assert.That(syntax.ToString(), Is.EqualTo(TestData.RefactoredSyntaxTree.GetRoot().ToString()));
        }
    }
}
