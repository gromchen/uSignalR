namespace uTasks
{
    public struct CancellationToken
    {
        private CancellationTokenSource _source;

        public CancellationToken(CancellationTokenSource source) : this()
        {
            _source = source;
        }

        public bool IsCancellationRequested { get; private set; }
    }
}