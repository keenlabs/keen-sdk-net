using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace Keen.Core
{
    // Simple version of AsyncLazy based on
    // https://blogs.msdn.microsoft.com/pfxteam/2011/01/15/asynclazyt/
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory)
            : base(() => Task.Factory.StartNew(valueFactory))
        { }

        public AsyncLazy(Func<Task<T>> taskFactory)
            : base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        { }

        // Make this class awaitable by passing through to Lazy's Value.
        public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); }
    }
}
