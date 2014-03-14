using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Keen.Core
{
    /// <summary>
    /// An instance of DynamicPropertyValue containing a delegate 
    /// can be added to the GlobalProperties collection. When AddEvent
    /// inserts GlobalProperties into an event, the delegate will be
    /// executed to provide the value of the property.
    /// </summary>
    public class DynamicPropertyValue : IDynamicPropertyValue
    {
        private Func<object> _value;

        /// <summary>
        /// Call the delegate that produces the property value
        /// </summary>
        /// <returns>The value produced by the delegate</returns>
        public object Value()
        {
            return _value();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">A delegate that will be called each time the property value is required</param>
        public DynamicPropertyValue(Func<object> value)
        {
            _value = value;
        }
    }
}
