using Keen.NetStandard.EventCache;
using System;
using System.Collections.Generic;


namespace Keen.NetStandard
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

    public class KeenInvalidEventException : KeenException
    {
        public KeenInvalidEventException() { }
        public KeenInvalidEventException(string message) : base(message) { }
        public KeenInvalidEventException(string message, Exception inner) : base(message, inner) { }
    }

    public class KeenListsOfNonPrimitivesNotAllowedException : KeenException
    {
        public KeenListsOfNonPrimitivesNotAllowedException() { }
        public KeenListsOfNonPrimitivesNotAllowedException(string message) : base(message) { }
        public KeenListsOfNonPrimitivesNotAllowedException(string message, Exception inner) : base(message, inner) { }    
    }

    public class KeenInvalidBatchException : KeenException
    {
        public KeenInvalidBatchException() { }
        public KeenInvalidBatchException(string message) : base(message) { }
        public KeenInvalidBatchException(string message, Exception inner) : base(message, inner) { }
    }

    public class KeenInternalServerErrorException : KeenException
    {
        public KeenInternalServerErrorException() { }
        public KeenInternalServerErrorException(string message) : base(message) { }
        public KeenInternalServerErrorException(string message, Exception inner) : base(message, inner) { }
    }
    
    public class KeenInvalidKeenNamespacePropertyException: KeenException
    {
        public KeenInvalidKeenNamespacePropertyException() { }
        public KeenInvalidKeenNamespacePropertyException(string message) : base(message) { }
        public KeenInvalidKeenNamespacePropertyException(string message, Exception inner) : base(message, inner) { }
    }
    
    public class KeenInvalidPropertyNameException: KeenException
    {
        public KeenInvalidPropertyNameException() { }
        public KeenInvalidPropertyNameException(string message) : base(message) { }
        public KeenInvalidPropertyNameException(string message, Exception inner) : base(message, inner) { }
    }

    public class KeenBulkException : KeenException
    {
        private IEnumerable<CachedEvent> _failedEvents;
        public IEnumerable<CachedEvent> FailedEvents { get { return _failedEvents; } protected set { ; } }
        public KeenBulkException(IEnumerable<CachedEvent> failedEvents) { _failedEvents = failedEvents; }
        public KeenBulkException(string message, IEnumerable<CachedEvent> failedEvents ) : base(message) { _failedEvents = failedEvents; }
    }
}
