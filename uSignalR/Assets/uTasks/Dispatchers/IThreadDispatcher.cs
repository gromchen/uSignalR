using System;

namespace uTasks.Dispatchers
{
    public interface IThreadDispatcher
    {
        void BeginInvoke(Action action);
    }
}