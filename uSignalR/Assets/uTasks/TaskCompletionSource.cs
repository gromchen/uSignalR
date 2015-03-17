using System;
using System.Collections;
using System.Collections.Generic;

namespace uTasks
{
    public class TaskCompletionSource<TResult>
    {
        public Task<TResult> Task { get; private set; }

        public TaskCompletionSource()
        {
            Task = new Task<TResult>();
        }

        public bool TrySetResult(TResult result)
        {
            var flag = Task.TrySetResult(result);

            if (flag == false && Task.IsCompleted == false)
            {
                MainThread.Current.BeginStart(WaitForCompletion());
            }

            return flag;
        }

        public bool TrySetCanceled()
        {
            return TrySetCanceled(new CancellationToken());
        }

        internal bool TrySetCanceled(CancellationToken tokenToRecord)
        {
            bool flag = Task.TrySetCanceled(tokenToRecord);

            if (flag == false && Task.IsCompleted == false)
            {
                MainThread.Current.BeginStart(WaitForCompletion());
            }

            return flag;
        }

        private IEnumerator WaitForCompletion()
        {
            while (Task.IsCompleted == false)
            {
                yield return null;
            }
        }

        public void SetResult(TResult result)
        {
            var flag = TrySetResult(result);

            if (flag == false)
            {
                throw new InvalidOperationException("Task is already completed.");
            }
        }

        public void SetException(Exception exception)
        {
            var flag = TrySetException(exception);

            if (flag == false)
            {
                throw new InvalidOperationException("Task is already completed.");
            }
        }

        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
                throw new InvalidOperationException("Task is already completed.");
        }

        public bool TrySetException(Exception exception)
        {
            var flag = Task.TrySetException(exception);

            if (flag == false && Task.IsCompleted == false)
            {
                MainThread.Current.BeginStart(WaitForCompletion());
            }

            return flag;
        }

        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null)
                throw new ArgumentNullException("exceptions");
            
            List<Exception> list = new List<Exception>();
            
            foreach (Exception exception in exceptions)
            {
                if (exception == null)
                    throw new ArgumentException("Exception is null.", "exceptions");

                list.Add(exception);
            }

            if (list.Count == 0)
                throw new ArgumentException("There is no exceptions.", "exceptions");
            
            bool flag = Task.TrySetException(list);

            if (flag == false && Task.IsCompleted == false)
            {
                MainThread.Current.BeginStart(WaitForCompletion());
            }

            return flag;
        }
    }
}