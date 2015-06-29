using System;
using System.Runtime.Serialization;

namespace uTasks
{
    [Serializable]
    public class TaskCanceledException : OperationCanceledException
    {
        [NonSerialized] private Task _canceledTask;

        public TaskCanceledException() : base("Task was canceled")
        {
        }

        public TaskCanceledException(string message) : base(message)
        {
        }

        public TaskCanceledException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public TaskCanceledException(Task task) : base("Task was canceled")
        {
            _canceledTask = task;
        }

        protected TaskCanceledException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public Task Task
        {
            get { return _canceledTask; }
        }
    }
}