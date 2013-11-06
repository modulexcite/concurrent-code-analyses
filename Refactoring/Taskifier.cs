using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace Refactoring
{
    public static class Taskifier
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Execute the APM-to-async/await refactoring for a given APM method invocation.
        /// </summary>
        /// <param name="document">The C# Document on which to operate/in which the Begin and End method calls are represented.</param>
        /// <param name="solution">The solution that contains the C# document.</param>
        /// <param name="workspace">The workspace to which the code in the syntax tree currently belongs, for formatting purposes.</param>
        /// <param name="index">The index number </param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorToTasks(this Document document, Solution solution, Workspace workspace, int index)
        {
            return null;
        }
    }
}
