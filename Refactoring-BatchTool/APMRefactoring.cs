using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NLog;
using Refactoring;
using Utilities;

namespace Refactoring_BatchTool
{
    public class APMRefactoring
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Workspace _workspace;

        public Solution OriginalSolution { get; private set; }
        public Solution RefactoredSolution { get; private set; }

        public bool Succesful
        {
            get
            {
                return !(
                    RefactoringExceptionWasThrown
                    || CompilationError
                    || PreconditionFailed
                    || NotImplementedExceptionWasThrown
                    || OtherExceptionWasThrown
                );
            }
        }

        public bool RefactoringExceptionWasThrown { get; private set; }
        public bool CompilationError { get; private set; }
        public bool PreconditionFailed { get; private set; }
        public bool NotImplementedExceptionWasThrown { get; private set; }
        public bool OtherExceptionWasThrown { get; private set; }

        public int NumMethodSymbolLookups { get; private set; }

        public APMRefactoring(Solution originalSolution, Workspace workspace)
        {
            _workspace = workspace;
            if (originalSolution == null) throw new ArgumentNullException("originalSolution");
            if (workspace == null) throw new ArgumentNullException("workspace");

            OriginalSolution = originalSolution;
        }

        public Solution SafelyRefactorSolution(Solution solution, Document document, int index)
        {
            var numInitialErrors = solution.CompilationErrorCount();

            var oldSolution = solution;
            try
            {
                solution = ExecuteRefactoring(document, solution, index);

                if (solution.CompilationErrorCount() > numInitialErrors)
                {
                    Logger.Error("Refactoring {0} caused new compilation errors. It will not be applied.", index);

                    Logger.Warn("=== ORIGINAL CODE ===\n{0}\n=== END ORIGINAL CODE ===",
                        document.GetTextAsync().Result);
                    Logger.Warn("=== REFACTORED CODE WITH ERROR(S) ===\n{0}\n=== END REFACTORED CODE WITH ERRORS ===",
                        solution.GetDocument(document.Id).GetTextAsync().Result);

                    Logger.Error("=== SOLUTION ERRORS ===");
                    foreach (
                        var diagnostic in solution.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        Logger.Error("=== Solution error: {0}", diagnostic);
                    }
                    Logger.Error("=== END OF SOLUTION ERRORS ===");

                    CompilationError = true;
                    RefactoredSolution = solution;
                    solution = oldSolution;
                }
                else
                {
                    RefactoredSolution = solution;

                    NumMethodSymbolLookups += SemanticModelExtensions.NumMethodSymbolLookups;
                    SemanticModelExtensions.ResetSymbolLookupCounter();
                }
            }
            catch (RefactoringException e)
            {
                Logger.Error("Refactoring failed: index={0}: {1}: {2}", index, e.Message, e);
                solution = oldSolution;

                RefactoringExceptionWasThrown = true;
            }
            catch (NotImplementedException e)
            {
                Logger.Error("Not implemented: index={0}: {1}: {2}", index, e.Message, e);
                solution = oldSolution;

                NotImplementedExceptionWasThrown = true;
            }
            catch (PreconditionException e)
            {
                Logger.Error("Precondition failed: {0}: {1}", e.Message, e);
                solution = oldSolution;

                PreconditionFailed = true;
            }
            catch (Exception e)
            {
                Logger.Error("Unhandled exception while refactoring: index={0}: {1}\n{2}", index, e.Message, e);
                solution = oldSolution;

                OtherExceptionWasThrown = true;
            }

            return solution;
        }

        private Solution ExecuteRefactoring(Document document, Solution solution, int index)
        {
            if (document == null) throw new ArgumentNullException("document");

            var syntax = ((SyntaxTree)document.GetSyntaxTreeAsync().Result).GetRoot();

            Logger.Info("Refactoring annotated document: index={0}", index);
            Logger.Debug("=== CODE TO REFACTOR ===\n{0}=== END OF CODE ===", syntax);

            var startTime = DateTime.UtcNow;

            var refactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(document, solution, _workspace, index);

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Debug("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Debug("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredSyntax.Format(_workspace));

            return solution.WithDocumentSyntaxRoot(document.Id, refactoredSyntax);
        }
    }
}
