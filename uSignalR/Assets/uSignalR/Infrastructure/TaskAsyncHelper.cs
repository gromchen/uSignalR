using uTasks;

namespace uSignalR.Infrastructure
{
    internal static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeTask<object>(null);

        private static Task<T> MakeTask<T>(T value)
        {
            return FromResult<T>(value);
        }

        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }
    }
}