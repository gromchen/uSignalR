using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace uTasks
{
    public class TaskCompletionSource<TResult>
    {
        public TaskCompletionSource()
        {
            Task = new Task<TResult>();
        }

        public Task<TResult> Task { get; private set; }

        public bool TrySetResult(TResult result)
        {
            return Task.TrySetResult(result);
        }

        public bool TrySetCanceled()
        {
            return TrySetCanceled(new CancellationToken());
        }

        internal bool TrySetCanceled(CancellationToken tokenToRecord)
        {
            return Task.TrySetCanceled(tokenToRecord);
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

        public void SetExceptions(IEnumerable<Exception> exceptions)
        {
            var flag = TrySetException(exceptions);

            if (flag == false)
                throw new InvalidOperationException("Task is already completed.");
        }

        public bool TrySetException(Exception exception)
        {
            return Task.TrySetException(exception);
        }

        public bool TrySetException([NotNull] IEnumerable<Exception> exceptions)
        {
            if (exceptions == null) throw new ArgumentNullException("exceptions");

            var list = new List<Exception>();

            foreach (var exception in exceptions)
            {
                if (exception == null)
                    throw new ArgumentException("Exception is null.", "exceptions");

                list.Add(exception);
            }

            if (list.Count == 0)
                throw new ArgumentException("There is no exceptions.", "exceptions");

            return Task.TrySetExceptions(list);
        }

        public void SetUnwrappedException(Exception exception)
        {
            var aggregateException = exception as AggregateException;

            if (aggregateException != null)
            {
                SetExceptions(aggregateException.InnerExceptions);
            }
            else
            {
                SetException(exception);
            }
        }

        public void SetCanceled()
        {
            var flag = TrySetCanceled();

            if (flag == false)
                throw new InvalidOperationException("Task is already completed.");
        }
    }
}