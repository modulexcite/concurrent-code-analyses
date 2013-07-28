namespace Utilities
{
    public class Constants
    {
        public static string[] BlockingMethodCalls = { "Thread.Sleep", "Task.WaitAll", "Task.WaitAny", "Task.Wait" };
    }
}