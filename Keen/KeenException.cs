using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core
{
    public class KeenException : Exception
    {
        public KeenException() { }
        public KeenException(string message) : base(message) { }
        public KeenException(string message, Exception inner) : base(message, inner) { }
    }

    public class KeenInvalidApiKeyException : KeenException
    {
        public KeenInvalidApiKeyException() { }
        public KeenInvalidApiKeyException(string message) : base(message) { }
        public KeenInvalidApiKeyException(string message, Exception inner) : base(message, inner) { }
    }

    public class KeenResourceNotFoundException : KeenException
    {
        public KeenResourceNotFoundException() { }
        public KeenResourceNotFoundException(string message) : base(message) { }
        public KeenResourceNotFoundException(string message, Exception inner) : base(message, inner) { }      
    }

    public class KeenNamespaceTypeException : KeenException
    {
        public KeenNamespaceTypeException() { }
        public KeenNamespaceTypeException(string message) : base(message) { }
        public KeenNamespaceTypeException(string message, Exception inner) : base(message, inner) { }
    }
}
