using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    public class RoslynUnitTestBase
    {
        // ReSharper disable InconsistentNaming
        protected static readonly MetadataReference mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");
        protected static readonly MetadataReference system = MetadataReference.CreateAssemblyReference("system");
        // ReSharper restore InconsistentNaming

        protected static SemanticModel CreateSimpleSemanticModel(SyntaxTree originalSyntaxTree)
        {
            var originalCompilation = Compilation.Create(
                "OriginalCompilation",
                syntaxTrees: new[] { originalSyntaxTree },
                references: new[] { mscorlib, system }
                );

            return originalCompilation.GetSemanticModel(originalSyntaxTree);
        }
    }
}