using System;
using System.Collections.Generic;

namespace uTasks
{
    public class AggregateException : Exception
    {
        private readonly List<Exception> _innerExceptions;

        public AggregateException()
        {
            _innerExceptions = new List<Exception>();
        }

        public IEnumerable<Exception> InnerExceptions
        {
            get { return _innerExceptions; }
        }

        public void AddInnerException(Exception exception)
        {
            _innerExceptions.Add(exception);
        }
    }
}