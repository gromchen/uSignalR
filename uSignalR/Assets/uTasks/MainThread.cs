using System;
using System.Collections;

namespace uTasks
{
    public abstract class MainThread
    {
        private static MainThread _current;

        /// <summary>
        ///     Property should be set in main thread.
        /// </summary>
        public static MainThread Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException("Please initialize task scheduler.");

                return _current;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _current = value;
            }
        }

        /// <summary>
        ///     Asynchronously invokes action in the main thread.
        /// </summary>
        public static void BeginInvoke(Action action)
        {
            Current.Schedule(action);
        }

        public abstract void Start(IEnumerator enumerator);

        protected abstract void Schedule(Action action);
        public abstract void BeginStart(IEnumerator enumerator);
        public abstract void BeginStop(IEnumerator enumerator);
    }
}