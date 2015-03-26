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

            switch (Status)
            {
                case TaskStatus.RanToCompletion:
                    task.Start();
                    break;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    return this;
                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndStart(task));
                    break;
            }

            return task;
        }

        public Task<TNewResult> ContinueWithTaskResult<TNewResult>(Func<Task<TResult>, TNewResult> function)
        {
            var task = new Task<TNewResult>(() => function(this));

            switch (Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    task.Start();
                    break;
                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndStart(task));
                    break;
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

            switch (Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    launchTask.Start();
                    break;
                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndStart(launchTask));
                    break;
            }

            return tcs.Task;
        }

        public Task ThenWithTaskAndWaitForInnerTask(Func<TResult, Task> function)
        {
            var newTask = new Task(() => { function(Result); });

            switch (Status)
            {
                case TaskStatus.RanToCompletion:
                    newTask.Start();
                    break;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    return this;
                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndStart(newTask));
                    break;
            }

            return newTask;
        }

        public Task<TNewResult> ThenWithTaskResult<TNewResult>(Func<TResult, TNewResult> function)
        {
            var newTask = new Task<TNewResult>(() => function(Result));

            switch (Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    newTask.Start();
                    break;
                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndStart(newTask));
                    break;
            }

            return newTask;
        }

        public void CompleteWithAction(Action<Task<TResult>> action)
        {
            switch (Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    action(this);
                    break;
                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndExecute(action));
                    break;
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
            catch (OperationCanceledException exception)
            {
                AddException(exception);
                Status = TaskStatus.Canceled;
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
            while (IsCompleted == false && IsFaulted == false && IsCanceled == false)
            {
                yield return null;
            }

            action(this);
        }

        #endregion
    }
}