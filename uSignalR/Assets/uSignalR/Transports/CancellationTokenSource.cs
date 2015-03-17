namespace uSignalR.Transports
{
    internal class CancellationTokenSource
    {
        public bool IsCancellationRequested { get; private set; }

        public void Cancel()
        {
            IsCancellationRequested = true;
        }
    }
}