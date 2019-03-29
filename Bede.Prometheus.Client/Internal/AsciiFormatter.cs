using Prometheus.Advanced.DataContracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Prometheus.Internal
{
    internal static class AsciiFormatter
    {
        // Use UTF-8 encoding, but provide the flag to ensure the Unicode Byte Order Mark is never
        // pre-pended to the output stream.
        private static readonly Encoding Encoding = new UTF8Encoding(false);

        public static void Format(Stream destination, IEnumerable<MetricFamily> metrics)
        {
            // Leave stream open as we are just using it, not the owner of the stream!
            using (var writer = new StreamWriter(destination, Encoding, bufferSize: 1024, leaveOpen: true))
            {
                writer.NewLine = "\n";

                foreach (var family in metrics)
                {
                    WriteFamily(writer, family);
                }
            }
        }

        private static void WriteFamily(StreamWriter writer, MetricFamily family)
        {
            // # HELP familyname helptext
            writer.Write("# HELP ");
            writer.Write(family.Name);
            writer.Write(" ");
            writer.WriteLine(family.Help);

            // # TYPE familyname type
            writer.Write("# TYPE ");
            writer.Write(family.Name);
            writer.Write(" ");
            writer.WriteLine(family.MetricType.ToString().ToLowerInvariant());

            foreach (var metric in family.Metric)
            {
                WriteMetric(writer, family, metric);
            }
        }

        private static void WriteMetric(StreamWriter writer, MetricFamily family, Metric metric)
        {
            var familyName = family.Name;

            if (metric.Gauge != null)
            {
                WriteMetricWithLabels(writer, familyName, null, metric.Gauge.Value, metric.Label);
            }
            else if (metric.Counter != null)
            {
                WriteMetricWithLabels(writer, familyName, null, metric.Counter.Value, metric.Label);
            }
            else if (metric.Summary != null)
            {
                WriteMetricWithLabels(writer, familyName, "_sum", metric.Summary.SampleSum, metric.Label);
                WriteMetricWithLabels(writer, familyName, "_count", metric.Summary.SampleCount, metric.Label);

                foreach (var quantileValuePair in metric.Summary.Quantile)
                {
                    var quantile = double.IsPositiveInfinity(quantileValuePair.Quantile) ? "+Inf" : quantileValuePair.Quantile.ToString(CultureInfo.InvariantCulture);

                    var quantileLabels = metric.Label.Concat(new[] { new LabelPair { Name = "quantile", Value = quantile } });

                    WriteMetricWithLabels(writer, familyName, null, quantileValuePair.Value, quantileLabels);
                }
            }
            else if (metric.Histogram != null)
            {
                WriteMetricWithLabels(writer, familyName, "_sum", metric.Histogram.SampleSum, metric.Label);
                WriteMetricWithLabels(writer, familyName, "_count", metric.Histogram.SampleCount, metric.Label);

                foreach (var bucket in metric.Histogram.Bucket)
                {
                    var value = double.IsPositiveInfinity(bucket.UpperBound) ? "+Inf" : bucket.UpperBound.ToString(CultureInfo.InvariantCulture);

                    var bucketLabels = metric.Label.Concat(new[] { new LabelPair { Name = "le", Value = value } });

                    WriteMetricWithLabels(writer, familyName, "_bucket", bucket.CumulativeCount, bucketLabels);
                }
            }
            else
            {
                throw new NotSupportedException($"Metric {familyName} cannot be exported because it does not carry data of any known type.");
            }
        }

        private static void WriteMetricWithLabels(StreamWriter writer, string familyName, string postfix, double value, IEnumerable<LabelPair> labels)
        {
            // familyname_postfix{labelkey1="labelvalue1",labelkey2="labelvalue2"} value
            writer.Write(familyName);

            if (postfix != null)
                writer.Write(postfix);

            if (labels?.Any() == true)
            {
                writer.Write('{');

                bool firstLabel = true;
                foreach (var label in labels)
                {
                    if (!firstLabel)
                        writer.Write(',');

                    firstLabel = false;

                    writer.Write(label.Name);
                    writer.Write("=\"");

                    // Have to escape the label values!
                    writer.Write(EscapeLabelValue(label.Value));

                    writer.Write('"');
                }

                writer.Write('}');
            }

            writer.Write(' ');
            writer.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        private static string EscapeLabelValue(string value)
        {
            return value
                    .Replace("\\", @"\\")
                    .Replace("\n", @"\n")
                    .Replace("\"", @"\""");
        }
    }
}
