using System;

namespace uTasks.Loggers
{
    public interface ILogger
    {
        void Log(Exception exception);
    }
}