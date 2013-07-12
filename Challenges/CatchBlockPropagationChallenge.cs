using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Challenges
{
    // In this example, the response variable in the Callback method should be made a parameter, 
    // and the EndGetResponse call should be moved to the EntryPoint method. However, EndGetResponse
    // might throw a WebException, so the catch block should be copied to the EntryPoint method.
    // But unfortunately, the catch block references identifiers from the Callback method scope that
    // encloses the try-catch statement, so it cannot be easily moved, without taking measures to
    // make those references available in EntryPoint as well.

    class CatchBlockPropagationChallenge
    {
        public static void EntryPoint()
        {
            var request = WebRequest.Create("url://...");
            request.BeginGetResponse(Callback, request);
        }

        private static void Callback(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            var x = 0;

            try
            {
                var response = request.EndGetResponse(result);

                // Do something with response, which might cause a WebException to be thrown.
            }
            catch (WebException e)
            {
                // Do something with x
            }
        }
    }
}
