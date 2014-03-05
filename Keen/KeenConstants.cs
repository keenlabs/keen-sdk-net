using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core
{
    public class KeenConstants
    {
        private const string serverAddress = "http://api.keen.io";
        public static string ServerAddress { get { return serverAddress; } protected set { ;} }

        private const string eventsCollectionResource = "events";
        public static string EventsCollectionResource { get { return eventsCollectionResource; } protected set { ;} }

        private const string apiVersion = "3.0";
        public static string ApiVersion { get { return apiVersion; } protected set { ;} }

        private const int collectionNameLengthLimit = 64;
        public static int CollectionNameLengthLimit { get { return collectionNameLengthLimit; } protected set { ;} }
    }
}
