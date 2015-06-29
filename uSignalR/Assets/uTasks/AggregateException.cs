using System;
using System.Collections.Generic;

namespace uTasks
{
    public class AggregateException : Exception
    {
        public AggregateException(params Exception[] exceptions)
        {
            InnerExceptions = exceptions;
        }

        public AggregateException(IEnumerable<Exception> exceptions)
        {
            InnerExceptions = exceptions;
        }

        public IEnumerable<Exception> InnerExceptions { get; private set; }
    }
}