using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class Constants
    {
        public static string[] BlockingMethodCalls = { "Thread.Sleep", "Task.WaitAll", "Task.WaitAny","Task.Wait" };
    }
}
