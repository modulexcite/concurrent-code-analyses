using Microsoft.CodeAnalysis;

namespace Refactoring
{
    /// <summary>
    /// Roslyn syntax annotation that marks a ExpressionStatementSyntax instance to be refactored.
    /// </summary>
    public class RefactorableAPMInstance : SyntaxAnnotation
    {
        public int Index { get; private set; }

        public RefactorableAPMInstance(int index)
        {
            Index = index;
        }
    }
}
