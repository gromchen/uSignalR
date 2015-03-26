using System;
using System.Collections;

namespace uTasks
{
    public class Task
    {
        private readonly Action _action;
        private CancellationToken _token;

        protected Task()
        {
            Status = TaskStatus.Created;
        }

        public Task(Action action) : this()
        {
            _action = action;
        }

        public Task(Action action, CancellationToken token) : this(action)
        {
            _token = token;
        }

        public AggregateException AggregateException { get; private set; }

        public bool IsCompleted
        {
            get { return Status == TaskStatus.RanToCompletion; }
        }

        public TaskStatus Status { get; protected set; }

        public bool IsFaulted
        {
            get { return Status == TaskStatus.Faulted; }
        }

        public bool IsCanceled
        {
            get { return Status == TaskStatus.Canceled || Status == TaskStatus.Faulted; }
        }

        protected void RecordInternalCancellationRequest(CancellationToken tokenToRecord,
            Exception cancellationException)
        {
            _token = tokenToRecord;
            AddException(cancellationException);
        }

        public virtual void Start()
        {
            Status = TaskStatus.Running;

            // todo: specification of token does nothing right now
            _action.BeginInvoke(ActionCallback, _token);
        }

        private void ActionCallback(IAsyncResult asyncResult)
        {
            try
            {
                _action.EndInvoke(asyncResult);
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

        internal void AddException(Exception exception)
        {
            if (AggregateException == null)
                AggregateException = new AggregateException();

            AggregateException.AddInnerException(exception);
        }

        internal void Finish(TaskStatus status = TaskStatus.RanToCompletion)
        {
            Status = status;
        }

        public Task ContinueWithTask(Action<Task> action)
        {
            return Then(new Task(() => action(this)));
        }

        public Task ThenWithTask(Action action)
        {
            return Then(new Task(action));
        }

        public Task ThenWithTask(Func<Task> function)
        {
            return Then(new Task<Task>(function));
        }

        public Task ThenWithTask<T1, T2>(Func<T1, T2, Task> successor, T1 arg1, T2 arg2)
        {
            return Then(new Task<Task>(() => successor(arg1, arg2)));
        }

        private Task Then(Task task)
        {
            switch (Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return this;

                case TaskStatus.RanToCompletion:
                    task.Start();
                    break;

                default:
                    MainThread.Current.BeginStart(WaitForCompletionAndStart(task));
                    break;
            }

            return task;
        }

        public void CompleteWithAction(Action<Task> action)
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

        public Task<TResult> ThenWithTaskResultAndWaitForInnerResult<TResult>(Func<Task<TResult>> function)
        {
            var tcs = new TaskCompletionSource<TResult>();

            var launchTask = new Task<Task<TResult>>(() =>
            {
                var newTask = function();
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

        #region Enumerations

        private IEnumerator WaitForCompletionAndExecute(Action<Task> action)
        {
            while (IsCompleted == false && IsFaulted == false && IsCanceled == false)
            {
                yield return null;
            }

            action(this);
        }

        protected IEnumerator WaitForCompletionAndStart(Task task)
        {
            while (IsCompleted == false && IsFaulted == false && IsCanceled == false)
            {
                yield return null;
            }

            task.Start();
        }

        #endregion
    }
}