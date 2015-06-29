using System;

namespace uTasks.Dispatchers
{
    public static class TaskExtensions
    {
        public static Task ThenInvoke(this Task task, IThreadDispatcher dispatcher, Action<Task> action)
        {
            // todo: move to vm and use there only
            return task.ContinueWith(t => { dispatcher.BeginInvoke(() => { action(t); }); });
        }

        public static Task ThenInvoke<T>(this Task<T> task, IThreadDispatcher dispatcher, Action<Task<T>> action)
        {
            return task.ContinueWith(t => { dispatcher.BeginInvoke(() => action(t)); });
        }
    }
}