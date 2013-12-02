using System;

namespace Utilities
{
    public class Enums
    {
        public enum ProjectType { WP7, WP8, NET4, NET45, NETOther };

        public enum AsyncDetected { APM = 0, EAP = 1, TAP = 2, Thread = 3, Threadpool = 4, AsyncDelegate = 5, BackgroundWorker = 6, Task = 7, ISynchronizeInvoke = 8, ControlInvoke = 9, Dispatcher = 10, ParallelFor=11, ParallelForEach=12, ParallelInvoke=13,None };

        [Flags]
        public enum SyncDetected
        {
            None = 0, APMReplacable = 1, EAPReplacable = 2, TAPReplacable = 4,
            APMEAPReplacable = APMReplacable | EAPReplacable,
            APMTAPReplacable = APMReplacable | TAPReplacable,
            EAPTAPReplacable = EAPReplacable | TAPReplacable,
            APMEAPTAPReplacable = APMReplacable | EAPReplacable | TAPReplacable
        };

        public enum CallbackType { None, Identifier, Lambda };

        public enum ThreadingNamespaceDetected { ThreadClass = 0, ThreadpoolClass = 1, OtherClass = 2, None };
        public enum TasksNamespaceDetected { TaskClass = 0, ParallelClass = 1, OtherClass = 2, None };
    }
}