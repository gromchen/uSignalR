using System;

namespace uTasks
{
    public class CancellationTokenSource
    {
        private bool _disposed;
        public bool IsCancellationRequested { get; private set; }

        public CancellationToken Token
        {
            get
            {
                ThrowIfDisposed();
                return new CancellationToken(this); 
            }
        }

        /// <summary>
        /// Throws an exception if the source has been disposed. 
        /// </summary> 
        internal void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, "Cancellation token source is disposed.");
        }

        public void Cancel()
        {
            IsCancellationRequested = true;
        }
    }
}