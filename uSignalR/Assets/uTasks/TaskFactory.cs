using System;
using System.Collections;

namespace uTasks
{
    public static class TaskFactory
    {
        public static Task StartNew(Action action)
        {
            var task = new Task(action);
            task.Start();
            return task;
        }

        public static Task StartNew(Action action, CancellationToken token)
        {
            var task = new Task(action, token);
            task.Start();
            return task;
        }

        public static Task<TResult> StartNew<TResult>(Func<TResult> function)
        {
            var task = new Task<TResult>(function);
            task.Start();
            return task;
        }

        public static Task<TResult> FromAsync<TResult>(
            Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod)
        {
            var task = new Task<TResult>();
            MainThread.Current.BeginStart(WaitForCompletion(task, beginMethod, endMethod));
            return task;
        }

        public static Task<T> FromError<T>(Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        #region Enumerations

        private static IEnumerator WaitForCompletion<TResult>(Task<TResult> task,
            Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod)
        {
            var asyncResult = beginMethod(null, null);

            while (asyncResult.IsCompleted == false)
            {
                yield return null;
            }

            var result = endMethod(asyncResult);
            task.TrySetResult(result);
        }

        #endregion
    }
}