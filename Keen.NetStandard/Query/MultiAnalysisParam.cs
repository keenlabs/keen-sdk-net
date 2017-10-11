
namespace Keen.Core.Query
{
    public sealed class MultiAnalysisParam
    {
        public sealed class Metric
        {
            private readonly string _value;
            public readonly string TargetProperty;
            private Metric(string value) { _value = value; }
            private Metric(string value, string targetProperty) { _value = value; TargetProperty = targetProperty;  }
            public override string ToString() { return _value; }
            public static implicit operator string(Metric value) { return value.ToString(); }

            public static Metric Count() { return new Metric("count"); }
            public static Metric CountUnique(string targetProperty) { return new Metric("count_unique", targetProperty); }
            public static Metric Sum(string targetProperty) { return new Metric("sum", targetProperty); }
            public static Metric Average(string targetProperty) { return new Metric("average", targetProperty); }
            public static Metric Minimum(string targetProperty) { return new Metric("minimum", targetProperty); }
            public static Metric Maximum(string targetProperty) { return new Metric("maximum", targetProperty); }
            public static Metric SelectUnique(string targetProperty) { return new Metric("select_unique", targetProperty); }
        }

        public string Label { get; private set; }
        public string Analysis { get; private set; }
        public string TargetProperty { get; private set; }

        /// <summary>
        /// MultiAnalysisParam defines one kind of analysis to run in a MultiAnalysis request.
        /// </summary>
        /// <param name="label">A user defined string that acts as a name for the analysis. 
        /// This will be returned in the results so the various analyses are easily identifiable.</param>
        /// <param name="analysis">The metric type.</param>
        public MultiAnalysisParam(string label, Metric analysis)
        {
            Label = label;
            Analysis = analysis;
            TargetProperty = analysis.TargetProperty;
        }
    }
}
