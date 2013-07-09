using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class SimpleAPMCase
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create("http://www.google.com/");

            request.BeginGetResponse(new AsyncCallback(CallBack), request);
        }

        private void CallBack(IAsyncResult result)
        {
            var request = (WebRequest)result.AsyncState;
            request.EndGetResponse(result);
        }
    }

    class SimpleAPMCaseWithoutCallback
    {
        public void FireAndForget()
        {
            var request = WebRequest.Create("http://www.google.com");
            IAsyncResult result = request.BeginGetResponse(null, request);
            var response = request.EndGetResponse(result);
        }
    }
}
