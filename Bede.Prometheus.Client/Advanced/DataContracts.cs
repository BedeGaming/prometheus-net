using System.Collections.Generic;

namespace Prometheus.Advanced.DataContracts
{
    public class LabelPair
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class GaugeValue
    {
        public double Value { get; set; }
    }

    public class CounterValue
    {
        public double Value { get; set; }
    }

    public class QuantileValue
    {
        public double Quantile { get; set; }
        public double Value { get; set; }
    }

    public class SummaryInfo
    {
        public ulong SampleCount { get; set; }
        public double SampleSum { get; set; }
        public List<QuantileValue> Quantile { get; } = new List<QuantileValue>();
    }

    public class UntypedValue
    {
        public double Value { get; set; }
    }

    public class HistogramInfo
    {
        public ulong SampleCount { get; set; }
        public double SampleSum { get; set; }
        public List<BucketInfo> Bucket { get; } = new List<BucketInfo>();
    }

    public class BucketInfo
    {
        public ulong CumulativeCount { get; set; }
        public double UpperBound { get; set; }
    }

    public class Metric
    {
        public List<LabelPair> Label { get; set; }
        public GaugeValue Gauge { get; set; }
        public CounterValue Counter { get; set; }
        public SummaryInfo Summary { get; set; }
        public UntypedValue Untyped { get; set; }
        public HistogramInfo Histogram { get; set; }
        public long TimestampMilliseconds { get; set; }
    }

    public class MetricFamily
    {
        public string Name { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public MetricType MetricType { get; set; } = MetricType.COUNTER;
        public List<Metric> Metric { get; } = new List<Metric>();
    }

    public enum MetricType
    {
        COUNTER = 0,
        GAUGE = 1,
        SUMMARY = 2,
        UNTYPED = 3,
        HISTOGRAM = 4
    }
}