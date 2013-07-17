using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class Enums
    {
        public enum ProjectType { WP7, WP8, NET4, NET45, NETOther };
        public enum AsyncDetected { APM = 0, EAP = 1, TAP = 2, Thread = 3, Threadpool = 4, AsyncDelegate = 5, BackgroundWorker = 6, TPL = 7, ISynchronizeInvoke = 8, ControlInvoke = 9, Dispatcher = 10, None };
        public enum SyncDetected { APMReplacable, EAPReplacable, TAPReplacable, APMEAPReplacable, APMTAPReplacable, EAPTAPReplacable, APMEAPTAPReplacable, None };
    }
}
