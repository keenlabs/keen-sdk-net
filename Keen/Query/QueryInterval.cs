
namespace Keen.Core.Query
{
    /// <summary>
    /// Provides values for interval query parameter
    /// </summary>
    public sealed class QueryInterval
    {
        private readonly string _value;
        private QueryInterval(string value) { _value = value; }
        private QueryInterval(string value, int n) { _value = string.Format(value, n); }
        public override string ToString() { return _value; }
        public static implicit operator string(QueryInterval value) { return value.ToString(); }

        /// <summary>
        /// breaks your timeframe into minute long chunks.
        /// </summary>
        public static QueryInterval Minutely() { return new QueryInterval("minutely"); }

        /// <summary>
        /// breaks your timeframe into hour long chunks.
        /// </summary>
        public static QueryInterval Hourly() { return new QueryInterval("hourly"); }

        /// <summary>
        /// breaks your timeframe into day long chunks.
        /// </summary>
        public static QueryInterval Daily() { return new QueryInterval("daily"); }

        /// <summary>
        /// breaks your timeframe into week long chunks.
        /// </summary>
        public static QueryInterval Weekly() { return new QueryInterval("weekly"); }

        /// <summary>
        /// breaks your timeframe into month long chunks.
        /// </summary>
        public static QueryInterval Monthly() { return new QueryInterval("monthly"); }

        /// <summary>
        /// breaks your timeframe into year long chunks.
        /// </summary>
        public static QueryInterval Yearly() { return new QueryInterval("yearly"); }


        /// <summary>
        /// breaks your timeframe into chunks of the specified length
        /// </summary>
        /// <param name="n">chunk length</param>
        public static QueryInterval EveryNMinutes(int n) { return new QueryInterval("every_{0}_minutes", n); }

        /// <summary>
        /// breaks your timeframe into chunks of the specified length
        /// </summary>
        /// <param name="n">chunk length</param>
        public static QueryInterval EveryNHours(int n) { return new QueryInterval("every_{0}_hours", n); }

        /// <summary>
        /// breaks your timeframe into chunks of the specified length
        /// </summary>
        /// <param name="n">chunk length</param>
        public static QueryInterval EveryNDays(int n) { return new QueryInterval("every_{0}_days", n); }

        /// <summary>
        /// breaks your timeframe into chunks of the specified length
        /// </summary>
        /// <param name="n">chunk length</param>
        public static QueryInterval EveryNWeeks(int n) { return new QueryInterval("every_{0}_weeks", n); }

        /// <summary>
        /// breaks your timeframe into chunks of the specified length
        /// </summary>
        /// <param name="n">chunk length</param>
        public static QueryInterval EveryNMonths(int n) { return new QueryInterval("every_{0}_months", n); }

        /// <summary>
        /// breaks your timeframe into chunks of the specified length
        /// </summary>
        /// <param name="n">chunk length</param>
        public static QueryInterval EveryNYears(int n) { return new QueryInterval("every_{0}_years", n); }
    }
}
