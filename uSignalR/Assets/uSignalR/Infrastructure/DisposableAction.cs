using System;

namespace uSignalR.Infrastructure
{
    internal class DisposableAction : IDisposable
    {
        private readonly Action _action;

        public DisposableAction(System.Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
