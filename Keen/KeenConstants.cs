using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core
{
    public class KeenConstants
    {
        private const string serverAddress = "https://api.keen.io";
        public static string ServerAddress { get { return serverAddress; } protected set { ;} }

        private const string eventsResource = "events";
        public static string EventsResource { get { return eventsResource; } protected set { ;} }

        private const string apiVersion = "3.0";
        public static string ApiVersion { get { return apiVersion; } protected set { ;} }

        private const int bulkBatchSize = 1000;
        public static int BulkBatchSize { get { return bulkBatchSize; } protected set { ;} }
    }
}
