using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        /// <returns>The SyntaxNode that contains all the replacmeents.</returns>
        public static T ReplaceAll<T>(this T node, ReplacementPair[] replacementPairs) where T : SyntaxNode
        {
            return node.ReplaceNodes(
                replacementPairs.Select(pair => pair.OldNode),
                (oldNode, newNode) => replacementPairs.First(pair => pair.OldNode == oldNode).NewNode
            );
        }

        /// <summary>
        /// Replace all old nodes in the given pairs with their corresponding new nodes.
        /// </summary>
        /// <typeparam name="T">Subtype of SyntaxNode that supports the
        /// replacement of descendent nodes.</typeparam>
        /// <param name="node">The SyntaxNode or subtype to operate on.</param>
        /// <param name="replacementPairs">The SyntaxNodeReplacementPair
        /// instances that each contain both the old node that is to be
        /// replaced, and the new node that will replace the old node.</param>
        /// <returns>The SyntaxNode that contains all the replacmeents.</returns>
        public static T ReplaceAll<T>(this T node, IEnumerable<ReplacementPair> replacementPairs) where T : SyntaxNode
        {
            return node.ReplaceNodes(
                replacementPairs.Select(pair => pair.OldNode),
                (oldNode, newNode) => replacementPairs.First(pair => pair.OldNode == oldNode).NewNode
            );
        }

        /// <summary>
        /// Pair of old and new SyntaxNodes for ReplaceAll.
        /// </summary>
        public sealed class ReplacementPair
        {
            /// <summary>The node that must be replaced.</summary>
            public readonly SyntaxNode OldNode;
            /// <summary>The node that will replace the old node.</summary>
            public readonly SyntaxNode NewNode;

            public ReplacementPair(SyntaxNode oldNode, SyntaxNode newNode)
            {
                if (oldNode == null) throw new ArgumentNullException("oldNode");
                if (newNode == null) throw new ArgumentNullException("newNode");

                OldNode = oldNode;
                NewNode = newNode;
            }
        }
    }
}
