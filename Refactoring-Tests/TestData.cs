using System;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    /// <summary>
    /// Different pieces of related data for the APM to async/await refactoring test.
    /// </summary>
    /// The different variables and methods in this class are closely related together.
    /// The methods are specifically tailored for the Original/Refactored strings, and
    /// care should be taken to change those in pairs.
    public class TestData
    {
        #region Assemblies

        private static readonly MetadataReference mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");

        #endregion

        #region Original syntax tree

        public static readonly SyntaxTree OriginalSyntaxTree = SyntaxTree.ParseText(OriginalCode);

        public static readonly Compilation OriginalCompilation = Compilation.Create(
            outputName: "OriginalCompilation",
            syntaxTrees: new[] { OriginalSyntaxTree },
            references: new[] { mscorlib });

        public static readonly SemanticModel OriginalSemanticModel =
            OriginalCompilation.GetSemanticModel(OriginalSyntaxTree);

        public static readonly InvocationExpressionSyntax APMInvocation = FindAPMMethodCall(OriginalSyntaxTree);

        #endregion

        #region Refactored syntax tree

        public static readonly SyntaxTree RefactoredSyntaxTree = SyntaxTree.ParseText(RefactoredCode);

        #endregion

        #region Original code and helpers

        private const string OriginalCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.google.com/"");
            request.BeginGetResponse(new AsyncCallback(CallBack), request);

            // Do something while GET request is in progress.
        }

        private void CallBack(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            var response = request.EndGetResponse(result);

            // Do something with the response.
        }
    }
}";

        private static InvocationExpressionSyntax FindAPMMethodCall(SyntaxTree syntaxTree)
        {
            return syntaxTree.GetRoot()
                             .DescendantNodes()
                             .OfType<InvocationExpressionSyntax>()
                             .First(invocation => invocation.ToString().Contains("Begin"));
        }

        #endregion

        #region Refactored code and helpers

        private const string RefactoredCode = @"using System;
using System.Net;

namespace TextInput
{
    class SimpleAPMCase
    {
        public async void FireAndForget()
        {
            var request = WebRequest.Create(""http://www.google.com/"");
            var task = request.GetResponseAsync().ConfigureAwait(false);

            // Do something while GET request is in progress.

            var response = await task;
            CallBack(response, request);
           
        }

        private void CallBack(WebResponse response, WebRequest request)
        {
            // Do something with the response.
        }
    }
}";

        #endregion
    }
}
