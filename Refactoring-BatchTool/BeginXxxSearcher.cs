using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using NLog;
using Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refactoring_BatchTool
{
    internal class BeginXxxSearcher : SyntaxWalker
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly SemanticModel _model;

        public BeginXxxSearcher(SemanticModel model)
        {
            BeginXxxSyntax = null;
            if (model == null) throw new ArgumentNullException("model");

            _model = model;
        }

        public InvocationExpressionSyntax BeginXxxSyntax { get; private set; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (BeginXxxSyntax != null)
            {
                Logger.Warn("BeginXxxSyntax is already set.");
                return;
            }

            MethodSymbol symbol;
            try
            {
                symbol = _model.LookupMethodSymbol(node);
            }
            catch (SymbolMissingException)
            {
                Logger.Debug("Failed to look up symbol for node, ignoring it: {0}", node);

                base.VisitInvocationExpression(node);

                return;
            }

            if (symbol.IsAPMBeginMethod())
            {
                Logger.Info("Found APM Begin method invocation: {0}", node);
                Logger.Info("  At {0}:{1}", node.SyntaxTree.FilePath, node.Span.Start);
                BeginXxxSyntax = node;
            }
            else
            {
                Logger.Trace("Non-APM-Begin method invocation: {0}", node);
                base.VisitInvocationExpression(node);
            }
        }
    }
}
