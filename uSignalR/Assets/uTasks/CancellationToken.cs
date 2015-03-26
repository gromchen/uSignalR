using System;

namespace uTasks
{
    public struct CancellationToken
    {
        private readonly CancellationTokenSource _source;

        public CancellationToken(CancellationTokenSource source) : this()
        {
            _source = source;
        }

        public bool IsCancellationRequested
        {
            get { return _source.IsCancellationRequested; }
        }

        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
                throw new OperationCanceledException("Operation was canceled.");
        }
    }
}