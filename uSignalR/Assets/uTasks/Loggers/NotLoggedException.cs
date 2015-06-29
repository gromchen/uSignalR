using System;

namespace uTasks.Loggers
{
    public class NotLoggedException : Exception
    {
        public NotLoggedException(string message) : base(message)
        {
        }

        public NotLoggedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}