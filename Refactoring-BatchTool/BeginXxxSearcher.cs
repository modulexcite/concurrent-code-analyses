using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
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
        public MethodSymbol BeginXxxSymbol { get; private set; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
        {
            if (invocation == null) throw new ArgumentNullException("invocation");

            if (BeginXxxSyntax != null)
            {
                return;
            }

            MethodSymbol symbol;
            try
            {
                symbol = _model.LookupMethodSymbol(invocation);
            }
            catch (SymbolMissingException)
            {
                Logger.Trace("Could not find symbol for node: {0}", invocation);
                base.VisitInvocationExpression(invocation);

                return;
            }

            if (symbol.IsAPMBeginMethod())
            {
                Logger.Info("Found APM Begin method invocation: {0}", invocation);
                Logger.Info("  At {0}:{1}",
                    invocation.SyntaxTree.FilePath,
                    invocation.SyntaxTree.GetLineSpan(invocation.FullSpan, true).StartLinePosition.Line
                );

                BeginXxxSyntax = invocation;
                BeginXxxSymbol = symbol;
            }
            else
            {
                Logger.Trace("Non-APM-Begin method invocation: {0}", invocation);
                base.VisitInvocationExpression(invocation);
            }
        }
    }
}
