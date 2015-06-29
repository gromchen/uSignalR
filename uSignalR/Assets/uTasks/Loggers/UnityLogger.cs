using System;
using uTasks.Dispatchers;

namespace uTasks.Loggers
{
    internal class UnityLogger : ILogger
    {
        private readonly IThreadDispatcher _dispatcher;

        public UnityLogger(IThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Log(Exception exception)
        {
            if (exception is NotLoggedException)
                return;

            _dispatcher.BeginInvoke(() => { throw exception; });
        }
    }
}