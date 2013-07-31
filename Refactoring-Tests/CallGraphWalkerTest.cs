using System.Linq;
using NUnit.Framework;
using Refactoring;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    public class CallGraphWalkerTest : RoslynUnitTestBase
    {
        private const string Code = @"namespace Test
{
    class X
    {
        public void M()
        {
            N();
            O();
        }

        private void N()
        {
            P(result => { });
            Q();
        }

        private void O()
        {
            P((result) => R());
            Q();
        }

        private int P(Callback callback) { }
        private void Q() { }
        private void R() { }

        private class Result { }
        private delegate void Callback(Result result);
    }
}";

        [Test]
        public void TestThatCallGraphIsWalked()
        {
            var syntaxTree = SyntaxTree.ParseText(Code);
            var model = CreateSimpleSemanticModel(syntaxTree);

            var m = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First(method => method.Identifier.ValueText.Equals("M"));

            var walker = new CallGraphWalker(model);
            walker.Walk(m);
        }
    }
}
