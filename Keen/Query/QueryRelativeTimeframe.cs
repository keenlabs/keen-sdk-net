
namespace Keen.Core.Query
{
    /// <summary>
    /// Provides values for relative timeframe query parameter.
    /// </summary>
    public sealed class QueryRelativeTimeframe : QueryTimeframe
    {
        private readonly string _value;
        private QueryRelativeTimeframe(string value) { _value = value; }
        private QueryRelativeTimeframe(string value, int n) { _value = string.Format(value, n); }
        public override string ToString() { return _value; }
        /// <summary>
        /// Creates a timeframe starting from the beginning of the current minute until now.
        /// </summary>
        public static QueryRelativeTimeframe ThisMinute() { return new QueryRelativeTimeframe("this_minute"); }
        /// <summary>
        /// Creates a timeframe starting from the beginning of the current hour until now.
        /// </summary>
        public static QueryRelativeTimeframe ThisHour() { return new QueryRelativeTimeframe("this_hour"); }
        /// <summary>
        /// Creates a timeframe starting from the beginning of the current day until now.
        /// </summary>
        public static QueryRelativeTimeframe ThisDay() { return new QueryRelativeTimeframe("this_day"); }
        /// <summary>
        /// Creates a timeframe starting from the beginning of the current week until now.
        /// </summary>
        public static QueryRelativeTimeframe ThisWeek() { return new QueryRelativeTimeframe("this_week"); }
        /// <summary>
        /// Creates a timeframe starting from the beginning of the current month until now.
        /// </summary>
        public static QueryRelativeTimeframe ThisMonth() { return new QueryRelativeTimeframe("this_month"); }
        /// <summary>
        /// Creates a timeframe starting from the beginning of the current year until now.
        /// </summary>
        public static QueryRelativeTimeframe ThisYear() { return new QueryRelativeTimeframe("this_year"); }
        /// <summary>
        /// All of the current minute and the previous completed n-1 minutes.
        /// </summary>
        public static QueryRelativeTimeframe ThisNMinutes(int n) { return new QueryRelativeTimeframe("this_{0}_minutes", n); }
        /// <summary>
        /// All of the current hour and the previous completed n-1 hours.
        /// </summary>
        public static QueryRelativeTimeframe ThisNHours(int n) { return new QueryRelativeTimeframe("this_{0}_hours", n); }
        /// <summary>
        /// All of the current day and the previous completed n-1 days.
        /// </summary>
        public static QueryRelativeTimeframe ThisNDays(int n) { return new QueryRelativeTimeframe("this_{0}_days", n); }
        /// <summary>
        /// All of the current week and the previous completed n-1 weeks.
        /// </summary>
        public static QueryRelativeTimeframe ThisNWeeks(int n) { return new QueryRelativeTimeframe("this_{0}_weeks", n); }
        /// <summary>
        /// All the current month and previous completed n-1 months.
        /// </summary>
        public static QueryRelativeTimeframe ThisNMonths(int n) { return new QueryRelativeTimeframe("this_{0}_months", n); }
        /// <summary>
        /// All the current year and previous completed n-1 years.
        /// </summary>
        public static QueryRelativeTimeframe ThisNYears(int n) { return new QueryRelativeTimeframe("this_{0}_years", n); }
        /// <summary>
        /// Gives a start of n-minutes before the most recent complete minute and an end at the most recent complete minute. 
        /// <para>Example: If right now it is 7:15:30pm and I specify “previous_3_minutes”, the timeframe would stretch from 7:12pm until 7:15pm.</para>
        /// </summary>
        public static QueryRelativeTimeframe PreviousNMinutes(int n) { return new QueryRelativeTimeframe("previous_{0}_minutes", n); }
        /// <summary>
        /// Gives a start of n-hours before the most recent complete hour and an end at the most recent complete hour. 
        /// <para>Example: If right now it is 7:15pm and I specify “previous_7_hours”, the timeframe would stretch from noon until 7:00pm.</para>
        /// </summary>
        public static QueryRelativeTimeframe PreviousNHours(int n) { return new QueryRelativeTimeframe("previous_{0}_hours", n); }
        /// <summary>
        /// Gives a starting point of n-days before the most recent complete day and an end at the most recent complete day. 
        /// <para>Example: If right now is Friday at 9:00am and I specify a timeframe of “previous_3_days”, the timeframe would stretch from Tuesday morning at 12:00am until Thursday night at midnight.</para>
        /// </summary>
        public static QueryRelativeTimeframe PreviousNDays(int n) { return new QueryRelativeTimeframe("previous_{0}_days", n); }
        /// <summary>
        /// Gives a start of n-weeks before the most recent complete week and an end at the most recent complete week. 
        /// <para>Example: If right now is Monday and I specify a timeframe of “previous_2_weeks”, the timeframe would stretch from three Sunday mornings ago at 12:00am until the most recent Sunday at 12:00am. (yesterday morning)</para>
        /// </summary>
        public static QueryRelativeTimeframe PreviousNWeeks(int n) { return new QueryRelativeTimeframe("previous_{0}_weeks", n); }
        /// <summary>
        /// Gives a start of n-months before the most recent completed month and an end at the most recent completed month. 
        /// <para>Example: If right now is the 5th of the month and I specify a timeframe of “previous_2_months”, the timeframe would stretch from the start of two months ago until the end of last month.</para>
        /// </summary>
        public static QueryRelativeTimeframe PreviousNMonths(int n) { return new QueryRelativeTimeframe("previous_{0}_months", n); }
        /// <summary>
        /// Gives a start of n-years before the most recent completed year and an end at the most recent completed year. 
        /// <para>Example: If right now is the June 5th and I specify a timeframe of “previous_2_years”, the timeframe would stretch from the start of two years ago until the end of last year.</para>
        /// </summary>
        public static QueryRelativeTimeframe PreviousNYears(int n) { return new QueryRelativeTimeframe("previous_{0}_years", n); }
        /// <summary>
        /// convenience for “previous_1_minute”
        /// </summary>
        public static QueryRelativeTimeframe PreviousMinute() { return new QueryRelativeTimeframe("previous_minute"); }
        /// <summary>
        /// convenience for “previous_1_hour”
        /// </summary>
        public static QueryRelativeTimeframe PreviousHour() { return new QueryRelativeTimeframe("previous_hour"); }
        /// <summary>
        ///  previous_day - convenience for “previous_1_day”
        /// </summary>
        public static QueryRelativeTimeframe Yesterday() { return new QueryRelativeTimeframe("yesterday"); }
        /// <summary>
        /// convenience for “previous_1_week”
        /// </summary>
        public static QueryRelativeTimeframe PreviousWeek() { return new QueryRelativeTimeframe("previous_week"); }
        /// <summary>
        /// convenience for “previous_1_months”
        /// </summary>
        public static QueryRelativeTimeframe PreviousMonth() { return new QueryRelativeTimeframe("previous_month"); }
        /// <summary>
        /// convenience for “previous_1_years”
        /// </summary>
        public static QueryRelativeTimeframe PreviousYear() { return new QueryRelativeTimeframe("previous_year"); }
    }
}
