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

        private const string eventsResource = "events";
        public static string EventsResource { get { return eventsResource; } protected set { ;} }

        private const string queriesResource = "queries";
        public static string QueriesResource { get { return queriesResource; } protected set { ;} }

        private const string queryCount = "count";
        public static string QueryCount { get { return queryCount; } protected set { ;} }

        private const string queryCountUnique = "count_unique";
        public static string QueryCountUnique { get { return queryCountUnique; } protected set { ;} }

        private const string queryMinimum = "minimum";
        public static string QueryMinimum { get { return queryMinimum; } protected set { ;} }

        private const string queryMaximum = "maximum";
        public static string QueryMaximum { get { return queryMaximum; } protected set { ;} }

        private const string queryAverage = "average";
        public static string QueryAverage { get { return queryAverage; } protected set { ;} }

        private const string querySum = "sum";
        public static string QuerySum { get { return querySum; } protected set { ;} }

        private const string querySelectUnique = "select_unique";
        public static string QuerySelectUnique { get { return querySelectUnique; } protected set { ;} }

        private const string queryExtraction = "extraction";
        public static string QueryExtraction { get { return queryExtraction; } protected set { ;} }

        private const string queryFunnel = "funnel";
        public static string QueryFunnel { get { return queryFunnel; } protected set { ;} }

        private const string queryMultiAnalysis = "multi_analysis";
        public static string QueryMultiAnalysis { get { return queryMultiAnalysis; } protected set { ;} }


        private const string queryParmEventCollection = "event_collection";
        public static string QueryParmEventCollection { get { return queryParmEventCollection; } protected set { ;} }

        private const string queryParmTargetProperty = "target_property";
        public static string QueryParmTargetProperty { get { return queryParmTargetProperty; } protected set { ;} }

        private const string queryParmTimeframe = "timeframe";
        public static string QueryParmTimeframe { get { return queryParmTimeframe; } protected set { ;} }

        private const string queryParmGroupBy = "group_by";
        public static string QueryParmGroupBy { get { return queryParmGroupBy; } protected set { ;} }

        private const string queryParmInterval = "interval";
        public static string QueryParmInterval { get { return queryParmInterval; } protected set { ;} }

        private const string queryParmTimezone = "timezone";
        public static string QueryParmTimezone { get { return queryParmTimezone; } protected set { ;} }

        private const string queryParmFilters = "filters";
        public static string QueryParmFilters { get { return queryParmFilters; } protected set { ;} }

        private const string queryParmEmail = "email";
        public static string QueryParmEmail { get { return queryParmEmail; } protected set { ;} }

        private const string queryParmLatest = "latest";
        public static string QueryParmLatest { get { return queryParmLatest; } protected set { ;} }

        private const string queryParmSteps = "steps";
        public static string QueryParmSteps { get { return queryParmSteps; } protected set { ;} }

        private const string queryParmAnalyses = "analyses";
        public static string QueryParmAnalyses { get { return queryParmAnalyses; } protected set { ;} }

        private const string apiVersion = "3.0";
        public static string ApiVersion { get { return apiVersion; } protected set { ;} }

        private const int bulkBatchSize = 1000;
        public static int BulkBatchSize { get { return bulkBatchSize; } protected set { ;} }
    }
}
