﻿using System;
using NLog;
using Roslyn.Compilers.CSharp;
using Utilities;

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

            var symbol = _model.LookupMethodSymbol(node);

            if (symbol != null)
            {
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
            else
            {
                Logger.Debug("Failed to look up symbol for node, ignoring it: {0}", node);
                base.VisitInvocationExpression(node);
            }
        }
    }
}
