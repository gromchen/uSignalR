using System;

namespace uTasks.Loggers
{
    public class NullLogger : ILogger
    {
        public void Log(Exception exception)
        {
            // do nothing
        }
    }
}