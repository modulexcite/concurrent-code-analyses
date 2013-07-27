using System;
using System.Threading.Tasks;

namespace Challenges
{
    internal class TryCatchBlockRefactoring
    {
    }

    public class OriginalProgram_TryCatchBlock
    {
        public void Action()
        {
            var request = new Request();
            request.BeginOperation(Callback, request);
        }

        private void Callback(IAsyncResult result)
        {
            try
            {
                var request = (Request)result.AsyncState;
                var response = request.EndOperation(result);
            }
            catch (Exception e)
            {
            }
        }
    }

    public class RefactoredProgram_TryCatchBlock
    {
        public async void Action()
        {
            var request = new Request();
            var task = request.OperationAsync();
            await Callback(task, request);
        }

        private async Task Callback(Task<Response> task, Request request)
        {
            try
            {
                var response = await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
            }
        }
    }
}