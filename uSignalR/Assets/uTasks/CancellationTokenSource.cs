namespace uTasks
{
    public class CancellationTokenSource
    {
        public bool IsCancellationRequested { get; private set; }

        public CancellationToken Token
        {
            get { return new CancellationToken(this); }
        }

        public void Cancel()
        {
            IsCancellationRequested = true;
        }
    }
}