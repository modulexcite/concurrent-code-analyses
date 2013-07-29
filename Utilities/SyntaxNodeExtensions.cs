using System;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace Utilities
{
    public static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Replace all old nodes in the given pairs with their corresponding new nodes.
        /// </summary>
        /// <typeparam name="T">Subtype of SyntaxNode that supports the 
        /// replacement of descendent nodes.</typeparam>
        /// <param name="node">The SyntaxNode or subtype to operate on.</param>
        /// <param name="replacementPairs">The SyntaxNodeReplacementPair 
        /// instances that each contain both the old node that is to be 
        /// replaced, and the new node that will replace the old node.</param>
        /// <returns></returns>
        public static T ReplaceAll<T>(this T node, SyntaxNodeReplacementPair[] replacementPairs) where T : SyntaxNode
        {
            return node.ReplaceNodes(
                replacementPairs.Select(pair => pair.OldNode),
                (oldNode, newNode) => replacementPairs.First(pair => pair.OldNode == oldNode).NewNode
            );
        }
    }

    /// <summary>
    /// Pair of old and new SyntaxNodes for ReplaceAll.
    /// </summary>
    public sealed class SyntaxNodeReplacementPair
    {
        /// <summary>The node that must be replaced.</summary>
        public readonly SyntaxNode OldNode;
        /// <summary>The node that will replace the old node.</summary>
        public readonly SyntaxNode NewNode;

        public SyntaxNodeReplacementPair(SyntaxNode oldNode, SyntaxNode newNode)
        {
            if (oldNode == null) throw new ArgumentNullException("oldNode");
            if (newNode == null) throw new ArgumentNullException("newNode");

            OldNode = oldNode;
            NewNode = newNode;
        }
    }
}
