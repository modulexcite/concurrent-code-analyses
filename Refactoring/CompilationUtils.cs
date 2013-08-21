using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refactoring
{
    // This could become an instantiable/inheritable class that encapsulates the details of building a Roslyn Compilation.
    // Right now, it just helps to create a basic Compilation that references the most basic libraries.
    public static class CompilationUtils
    {
        // ReSharper disable InconsistentNaming
        private static readonly MetadataReference mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");
        private static readonly MetadataReference system = MetadataReference.CreateAssemblyReference("system");
        // ReSharper restore InconsistentNaming

        public static Compilation CreateCompilation(params SyntaxTree[] originalSyntaxTrees)
        {
            if (originalSyntaxTrees == null) throw new ArgumentNullException("originalSyntaxTrees");

            var originalCompilation = Compilation.Create(
                "Compilation",
                syntaxTrees: originalSyntaxTrees,
                references: new[] { mscorlib, system }
            );

            return originalCompilation;
        }
    }
}
