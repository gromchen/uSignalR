using System;
using System.Collections.Generic;

namespace uTasks
{
    public class Task<TResult> : Task
    {
        private readonly Func<TResult> _function;

        public Task()
        {
        }

        public Task(Func<TResult> function)
        {
            _function = function;
        }

        public TResult Result { get; private set; }

        protected override void Process()
        {
            Result = _function();
        }

        #region ContinueWith

        public Task ContinueWith(Action<Task<TResult>> action)
        {
            return ContinueWith(new Task(() => action(this)));
        }

        /// <remarks>
        ///     Function has to be called <see cref="ContinueWithNewResult{TNewResult}" /> since otherwise Unity compiler can't
        ///     distinguish <see cref="Task.ContinueWith" /> functions.
        /// </remarks>
        public Task<TNewResult> ContinueWithNewResult<TNewResult>(Func<Task<TResult>, TNewResult> function)
        {
            return ContinueWithNewResult(new Task<TNewResult>(() => function(this)));
        }

        private Task<TNewResult> ContinueWithNewResult<TNewResult>(Task<TNewResult> task)
        {
            lock (ContinuationLock)
            {
                switch (Status)
                {
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                    case TaskStatus.RanToCompletion:
                        task.Start();
                        return task;
                    default:
                        Continuations.Add(task);
                        return task;
                }
            }
        }

        #endregion

        #region Try

        internal bool TrySetResult(TResult result)
        {
            if (IsCompleted) return false;

            Result = result;
            Finish(TaskStatus.RanToCompletion);
            return true;
        }

        internal bool TrySetCanceled(CancellationToken tokenToRecord, Exception cancellationException = null)
        {
            if (IsCompleted) return false;

            RecordInternalCancellationRequest(tokenToRecord, cancellationException);
            Finish(TaskStatus.Canceled);
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            if (IsCompleted) return false;

            AddException(exception);
            Finish(TaskStatus.Faulted);
            return true;
        }

        public bool TrySetExceptions(IEnumerable<Exception> exceptions)
        {
            if (IsCompleted) return false;

            foreach (var exception in exceptions)
            {
                AddException(exception);
            }

            Finish(TaskStatus.Faulted);
            return true;
        }

        #endregion

        #region Then

        public Task Then(Func<TResult, Task> function)
        {
            return ContinueWith(new Task(() => function(Result)));
        }

        public Task<TNewResult> Then<TNewResult>(Func<TResult, Task<TNewResult>> function)
        {
            var source = new TaskCompletionSource<TNewResult>();
            Action launch = () => { function(Result).ContinueWith(t => source.SetResult(t.Result)); };

            switch (Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                case TaskStatus.RanToCompletion:
                    launch();
                    return source.Task;
                default:
                    base.ContinueWith(t => launch());
                    return source.Task;
            }
        }

        public Task<TNewResult> Then<TNewResult>(Func<TResult, TNewResult> function)
        {
            return ContinueWithNewResult(new Task<TNewResult>(() => function(Result)));
        }

        #endregion
    }
}