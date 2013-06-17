using System.IO;
using Roslyn.Compilers.CSharp;

namespace Analysis
{
    class InterestingCallsCollector
    {
        private readonly StreamWriter _writer;

        public InterestingCallsCollector(StreamWriter writer)
        {
            _writer = writer;
        }

        public void WriteCallTrace(MethodDeclarationSyntax node, int n)
        {
            for (var i = 0; i < n; i++)
                _writer.Write(" ");
            _writer.Write(node.Identifier + " " + n + "\r\n");
        }

        public void PrintAppNameHeader(string appName)
        {
            var header = " #################\r\n" + appName + "\r\n#################\r\n";
            _writer.Write(header);
        }

        public void PrintUnresolvedMethod(string methodCallName)
        {
            var unresolved = " //Unresolved// " + methodCallName + " \\\\\\\\\\\r\n";
            _writer.Write(unresolved);
        }

        public void PrintDispatcherOccurrence(MethodSymbol methodCallSymbol)
        {
            var dispatcher = " //Dispatcher// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(dispatcher);
        }

        public void PrintControlInvokeOccurrence(MethodSymbol methodCallSymbol)
        {
            var controlInvoke = " //Form.Control// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(controlInvoke);
        }

        public void PrintEAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var eap = " //EAP// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(eap);
        }

        public void PrintTAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var tap = " //TAP// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(tap);
        }

        public void PrintAPMCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var apm = " //APM// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(apm);
        }

        public void PrintThreadStartOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadStart = " //Thread// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(threadStart);
        }

        public void PrintBackgroundWorkerRunWorkerAsyncOccurrence(MethodSymbol methodCallSymbol)
        {
            var backgroundWorker = " //BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(backgroundWorker);
        }

        public void PrintThreadPoolQueueUserWorkItemOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadpool = " //ThreadPool// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            _writer.Write(threadpool);
        }

        public void PrintThreadPoolQueueUserWorkItemWithSynchronizationContextOccurrence(InvocationExpressionSyntax methodCall)
        {
            var threadpoolContext = " //ThreadPool with Context// " + methodCall + " \\\\\\\\\\\r\n";
            _writer.Write(threadpoolContext);
        }

        public void PrintThreadPoolQueueUserWorkItemWithDispatcherOccurrence(InvocationExpressionSyntax methodCall)
        {
            var threadpoolDispatcher = " //ThreadPool with Dispatcher// " + methodCall + " \\\\\\\\\\\r\n";
            _writer.Write(threadpoolDispatcher);
        }
    }
}
