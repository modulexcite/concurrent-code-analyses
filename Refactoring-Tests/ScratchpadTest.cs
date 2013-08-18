using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Refactoring;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    [TestFixture]
    class ScratchpadTest
    {
        [Test]
        public void Test()
        {
            var cu = Syntax.ParseCompilationUnit("using System; namespace X { class C { public void M () { } } }");
            var cls = cu.DescendantNodes().First(node => node.Kind.Equals(SyntaxKind.ClassDeclaration));

            cls = cls.WithAdditionalAnnotations(new InvocationOfInterestAnnotation());

            //var tree = cu.SyntaxTree;
            var tree = SyntaxTree.Create(cu);

            Console.WriteLine("tree: {0}", tree);

            Assert.That(tree.GetRoot().DescendantNodes().First(m => m.Kind == SyntaxKind.ClassDeclaration), Is.SameAs(cls));
        }
    }

    internal class StatementMarker : SyntaxAnnotation
    {
    }
}
