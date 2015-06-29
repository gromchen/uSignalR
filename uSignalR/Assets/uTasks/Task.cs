using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using uTasks.Loggers;

namespace uTasks
{
    public class Task
    {
        private static ILogger _logger;
        private readonly Action _action;
        private readonly IList<Exception> _exceptions = new List<Exception>();
#if UNITY_EDITOR
        private readonly ManualResetEvent _finishEvent = new ManualResetEvent(false);
#endif
        protected readonly object ContinuationLock = new object();
        protected readonly List<Task> Continuations = new List<Task>();

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
            // todo: implement cancellation
            CancellationToken = token;
        }

        public static ILogger Logger
        {
            get
            {
                if (_logger != null)
                    return _logger;

                _logger = new NullLogger();
                return _logger;
            }
            set { _logger = value; }
        }

        public AggregateException Exception
        {
            get
            {
                AggregateException exception = null;

                if (IsFaulted)
                    exception = GetExceptions(false);

                if (exception != null && IsFaulted == false)
                    throw new InvalidOperationException("Returning non-null value when not Faulted");

                return exception;
            }
        }

        public bool IsCompleted
        {
            get { return Status == TaskStatus.RanToCompletion; }
        }

        public TaskStatus Status { get; private set; }

        public bool IsFaulted
        {
            get { return Status == TaskStatus.Faulted; }
        }

        public bool IsCanceled
        {
            get { return Status == TaskStatus.Canceled; }
        }

        public CancellationToken CancellationToken { get; private set; }

        private AggregateException GetExceptions(bool includeTaskCanceledExceptions)
        {
            var exceptions = new List<Exception>(_exceptions);

            if (includeTaskCanceledExceptions && IsCanceled)
            {
                exceptions.Add(new TaskCanceledException(this));
            }

            return exceptions.Any()
                ? new AggregateException(exceptions)
                : null;
        }

        protected void RecordInternalCancellationRequest(CancellationToken tokenToRecord,
            Exception cancellationException)
        {
            CancellationToken = tokenToRecord;
            AddException(cancellationException);
        }

        public void Start()
        {
            Status = TaskStatus.Running;
            ThreadPool.QueueUserWorkItem(state => { SafelyProcess(); });
        }

        private void SafelyProcess()
        {
            try
            {
                Process();
                Finish(TaskStatus.RanToCompletion);
            }
            catch (OperationCanceledException exception)
            {
                AddException(exception);
                Finish(TaskStatus.Canceled);
            }
            catch (Exception exception)
            {
                Logger.Log(exception);
                AddException(exception);
                Finish(TaskStatus.Faulted);
            }
        }

        protected virtual void Process()
        {
            // todo: use cancellation token
            _action();
        }

        internal void AddException(Exception exception)
        {
            _exceptions.Add(exception);
        }

        internal void Finish(TaskStatus status)
        {
            Status = status;

            lock (ContinuationLock)
            {
                foreach (var continuation in Continuations)
                {
                    continuation.Start();
                }
            }

#if UNITY_EDITOR
            _finishEvent.Set();
#endif
        }

#if UNITY_EDITOR
        /// <remarks>
        ///     Use with tests only.
        /// </remarks>
        public void Wait()
        {
            if (IsCompleted)
                return;

            _finishEvent.WaitOne();

            var aggregateException = GetExceptions(true);

            if (aggregateException != null)
            {
                throw aggregateException.InnerExceptions.First();
            }
        }
#endif

        #region Run

        public static Task Run(Action action)
        {
            var task = new Task(action);
            task.Start();
            return task;
        }

        public static Task Run(Action action, CancellationToken token)
        {
            var task = new Task(action, token);
            task.Start();
            return task;
        }

        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            var task = new Task<TResult>(function);
            task.Start();
            return task;
        }

        #endregion

        #region From

        public static Task FromResult()
        {
            var source = new TaskCompletionSource<object>();
            source.SetResult(new object());
            return source.Task;
        }

        public static Task<T> FromResult<T>(T value)
        {
            var source = new TaskCompletionSource<T>();
            source.SetResult(value);
            return source.Task;
        }

        public static Task<T> FromError<T>(Exception exception)
        {
            var source = new TaskCompletionSource<T>();
            source.SetUnwrappedException(exception);
            return source.Task;
        }

        public static Task<TResult> FromCancel<TResult>()
        {
            var source = new TaskCompletionSource<TResult>();
            source.SetCanceled();
            return source.Task;
        }

        #endregion

        #region ContinueWith

        public Task ContinueWith(Action<Task> action)
        {
            return ContinueWith(new Task(() => action(this)));
        }

        protected Task ContinueWith(Task task)
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

        #region Then

        public Task Then(Action action)
        {
            return ContinueWith(new Task(action));
        }

        public Task Then(Func<Task> function)
        {
            return ContinueWith(new Task<Task>(function));
        }

        public Task Then<T1, T2>(Func<T1, T2, Task> successor, T1 arg1, T2 arg2)
        {
            return ContinueWith(new Task<Task>(() => successor(arg1, arg2)));
        }

        public Task<TResult> Then<TResult>(Func<Task<TResult>> function)
        {
            var source = new TaskCompletionSource<TResult>();
            Action launch = () => { function().ContinueWith(t => source.SetResult(t.Result)); };

            switch (Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                case TaskStatus.RanToCompletion:
                    launch();
                    return source.Task;
                default:
                    ContinueWith(t => launch());
                    return source.Task;
            }
        }

        #endregion
    }
}