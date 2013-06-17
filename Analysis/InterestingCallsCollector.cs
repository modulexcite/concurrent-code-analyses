using System.IO;
using Roslyn.Compilers.CSharp;
using Utilities;
using System;

namespace Analysis
{
    class InterestingCallsCollector
    {
        private readonly StreamWriter _writer;

        public InterestingCallsCollector(StreamWriter writer)
        {
            _writer = writer;
        }

        public void PrintAppNameHeader(string appName)
        {
            var header = " #################\r\n" + appName + "\r\n#################\r\n";
            WriteResult(header);
        }

        public void PrintUnresolvedMethod(string methodCallName)
        {
            var unresolved = " //Unresolved// " + methodCallName + " \\\\\\\\\\\r\n";
            WriteResult(unresolved);
        }

        public void PrintDispatcherOccurrence(MethodSymbol methodCallSymbol)
        {
            var dispatcher = " //Dispatcher// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(dispatcher);
        }

        public void PrintControlInvokeOccurrence(MethodSymbol methodCallSymbol)
        {
            var controlInvoke = " //Form.Control// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(controlInvoke);
        }

        public void PrintEAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var eap = " //EAP// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(eap);
        }

        public void PrintTAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var tap = " //TAP// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(tap);
        }

        public void PrintAPMCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var apm = " //APM// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(apm);
        }

        public void PrintThreadStartOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadStart = " //Thread// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(threadStart);
        }

        public void PrintBackgroundWorkerRunWorkerAsyncOccurrence(MethodSymbol methodCallSymbol)
        {
            var backgroundWorker = " //BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(backgroundWorker);
        }

        public void PrintThreadPoolQueueUserWorkItemOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadpool = " //ThreadPool// " + methodCallSymbol + " \\\\\\\\\\\r\n";
            WriteResult(threadpool);
        }

        public void PrintThreadPoolQueueUserWorkItemWithSynchronizationContextOccurrence(InvocationExpressionSyntax methodCall)
        {
            var threadpoolContext = " //ThreadPool with Context// " + methodCall + " \\\\\\\\\\\r\n";
            WriteResult(threadpoolContext);
        }

        public void PrintThreadPoolQueueUserWorkItemWithDispatcherOccurrence(InvocationExpressionSyntax methodCall)
        {
            var threadpoolDispatcher = " //ThreadPool with Dispatcher// " + methodCall + " \\\\\\\\\\\r\n";
            WriteResult(threadpoolDispatcher);
        }

        private void WriteResult(string threadpoolDispatcher)
        {
            _writer.Write(threadpoolDispatcher);
        }
    }
}
