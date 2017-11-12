using Keen.Core.Query;


namespace Keen.Core.Dataset
{
    internal static class QueryDefinitionExtensions
    {
        public static void ValidateForCachedDataset(this QueryDefinition query)
        {
            if (string.IsNullOrWhiteSpace(query.AnalysisType))
            {
                throw new KeenException("QueryDefinition must have an analysis type");
            }

            if (string.IsNullOrWhiteSpace(query.EventCollection))
            {
                throw new KeenException("QueryDefinition must specify an event collection");
            }

            if (string.IsNullOrWhiteSpace(query.Timeframe))
            {
                throw new KeenException("QueryDefinition must specify a timeframe");
            }

            if (string.IsNullOrWhiteSpace(query.Interval))
            {
                throw new KeenException("QueryDefinition must specify an interval");
            }
        }
    }
}
