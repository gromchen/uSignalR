using System;
using System.Collections;
using System.Collections.Generic;

namespace uTasks
{
    public class Task<TResult> : Task
    {
        private readonly Func<TResult> _function;

        public Task(Func<TResult> function)
        {
            _function = function;
        }

        public Task()
        {
        }

        public TResult Result { get; private set; }

        internal bool TrySetCanceled(CancellationToken tokenToRecord, Exception cancellationException = null)
        {
            Finish(TaskStatus.Canceled);
            RecordInternalCancellationRequest(tokenToRecord, cancellationException);
            return true;
        }

        public Task ContinueWithTask(Action<Task<TResult>> action)
        {
            var task = new Task(() => action(this));

            if (IsCompleted)
            {
                task.Start();
            }
            else
            {
                MainThread.Current.BeginStart(WaitForCompletionAndStart(task));
            }

            return task;
        }

        public Task<TNewResult> ContinueWithTaskResult<TNewResult>(Func<Task<TResult>, TNewResult> function)
        {
            var task = new Task<TNewResult>(() => function(this));

            if (IsCompleted)
            {
                task.Start();
            }
            else
            {
                MainThread.Current.BeginStart(WaitForCompletionAndStart(task));
            }

            return task;
        }

        internal bool TrySetResult(TResult result)
        {
            if (IsCompleted)
            {
                return false;
            }

            Result = result;

            Finish();
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            AddException(exception);
            Finish();
            return true;
        }

        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            foreach (var exception in exceptions)
            {
                AddException(exception);
            }

            Finish();
            return true;
        }

        public Task<TNewResult> ThenWithTaskResultAndWaitForInnerResult<TNewResult>(
            Func<TResult, Task<TNewResult>> function)
        {
            var tcs = new TaskCompletionSource<TNewResult>();

            var launchTask = new Task<Task<TNewResult>>(() =>
            {
                var newTask = function(Result);
                newTask.CompleteWithAction(t => tcs.SetResult(t.Result));
                return newTask;
            });

            if (IsCompleted)
            {
                launchTask.Start();
            }
            else
            {
                MainThread.Current.BeginStart(WaitForCompletionAndStart(launchTask));
            }

            return tcs.Task;
        }

        public Task ThenWithTaskAndWaitForInnerTask(Func<TResult, Task> function)
        {
            var newTask = new Task(() => { function(Result); });

            if (IsCompleted)
            {
                newTask.Start();
            }
            else
            {
                MainThread.Current.BeginStart(WaitForCompletionAndStart(newTask));
            }

            return newTask;
        }

        public Task<TNewResult> ThenWithTaskResult<TNewResult>(Func<TResult, TNewResult> function)
        {
            var newTask = new Task<TNewResult>(() => function(Result));

            if (IsCompleted)
            {
                newTask.Start();
            }
            else
            {
                MainThread.Current.BeginStart(WaitForCompletionAndStart(newTask));
            }

            return newTask;
        }

        public void CompleteWithAction(Action<Task<TResult>> action)
        {
            if (IsCompleted)
            {
                action(this);
            }
            else
            {
                MainThread.Current.BeginStart(WaitForCompletionAndExecute(action));
            }
        }

        public override void Start()
        {
            Status = TaskStatus.Running;
            _function.BeginInvoke(FunctionCallback, null);
        }

        private void FunctionCallback(IAsyncResult asyncResult)
        {
            try
            {
                Result = _function.EndInvoke(asyncResult);
                Status = TaskStatus.RanToCompletion;
            }
            catch (Exception exception)
            {
                AddException(exception);
                Status = TaskStatus.Faulted;
            }
        }

        #region Enumerations

        private IEnumerator WaitForCompletionAndExecute(Action<Task<TResult>> action)
        {
            while (IsCompleted == false)
            {
                yield return null;
            }

            action(this);
        }

        #endregion
    }
}