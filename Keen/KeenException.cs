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


}
