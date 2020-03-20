using Prometheus.Advanced.DataContracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometheus.Internal
{
    internal static class Serializer
    {
        public static async Task SerializeAsync(StreamWriter writer, IEnumerable<MetricFamily> metrics)
        {
            var newLine = writer.NewLine;

            try
            {
                writer.NewLine = "\n";

                foreach (var family in metrics)
                {
                    await WriteFamilyAsync(writer, family).ConfigureAwait(false);
                }
            }
            finally
            {
                writer.NewLine = newLine;
            }
        }

        private static async Task WriteFamilyAsync(StreamWriter writer, MetricFamily family)
        {
            // # HELP familyname helptext
            await writer.WriteAsync("# HELP ").ConfigureAwait(false);
            await writer.WriteAsync(family.Name).ConfigureAwait(false);
            await writer.WriteAsync(" ").ConfigureAwait(false);
            await writer.WriteLineAsync(family.Help).ConfigureAwait(false);

            // # TYPE familyname type
            await writer.WriteAsync("# TYPE ").ConfigureAwait(false);
            await writer.WriteAsync(family.Name).ConfigureAwait(false);
            await writer.WriteAsync(" ").ConfigureAwait(false);
            await writer.WriteLineAsync(family.MetricType.ToString().ToLowerInvariant()).ConfigureAwait(false);

            foreach (var metric in family.Metric)
            {
                await WriteMetricAsync(writer, family, metric).ConfigureAwait(false);
            }
        }

        private static async Task WriteMetricAsync(StreamWriter writer, MetricFamily family, Metric metric)
        {
            var familyName = family.Name;

            if (metric.Gauge != null)
            {
                await WriteMetricWithLabelsAsync(writer, familyName, null, metric.Gauge.Value, metric.Label)
                    .ConfigureAwait(false);
            }
            else if (metric.Counter != null)
            {
                await WriteMetricWithLabelsAsync(writer, familyName, null, metric.Counter.Value, metric.Label)
                    .ConfigureAwait(false);
            }
            else if (metric.Summary != null)
            {
                await WriteMetricWithLabelsAsync(writer, familyName, "_sum", metric.Summary.SampleSum, metric.Label)
                    .ConfigureAwait(false);
                await WriteMetricWithLabelsAsync(writer, familyName, "_count", metric.Summary.SampleCount, metric.Label)
                    .ConfigureAwait(false);

                foreach (var quantileValuePair in metric.Summary.Quantile)
                {
                    var quantile = double.IsPositiveInfinity(quantileValuePair.Quantile) ? "+Inf" : quantileValuePair.Quantile.ToString(CultureInfo.InvariantCulture);

                    var quantileLabels = metric.Label.Concat(new[] { new LabelPair { Name = "quantile", Value = quantile } });

                    await WriteMetricWithLabelsAsync(writer, familyName, null, quantileValuePair.Value, quantileLabels)
                        .ConfigureAwait(false);
                }
            }
            else if (metric.Histogram != null)
            {
                await WriteMetricWithLabelsAsync(writer, familyName, "_sum", metric.Histogram.SampleSum, metric.Label)
                    .ConfigureAwait(false);
                await WriteMetricWithLabelsAsync(writer, familyName, "_count", metric.Histogram.SampleCount, metric.Label)
                    .ConfigureAwait(false);

                foreach (var bucket in metric.Histogram.Bucket)
                {
                    var value = double.IsPositiveInfinity(bucket.UpperBound)
                        ? "+Inf"
                        : bucket.UpperBound.ToString(CultureInfo.InvariantCulture);

                    var bucketLabels = metric.Label.Concat(new[] { new LabelPair { Name = "le", Value = value } });

                    await WriteMetricWithLabelsAsync(writer, familyName, "_bucket", bucket.CumulativeCount, bucketLabels)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                throw new NotSupportedException($"Metric {familyName} cannot be exported because it does not carry data of any known type.");
            }
        }

        private static async Task WriteMetricWithLabelsAsync(
            StreamWriter writer,
            string familyName,
            string postfix,
            double value,
            IEnumerable<LabelPair> labels)
        {
            // familyname_postfix{labelkey1="labelvalue1",labelkey2="labelvalue2"} value
            await writer.WriteAsync(familyName).ConfigureAwait(false);

            if (postfix != null)
                await writer.WriteAsync(postfix).ConfigureAwait(false);

            if (labels?.Any() == true)
            {
                await writer.WriteAsync('{').ConfigureAwait(false);

                bool firstLabel = true;
                foreach (var label in labels)
                {
                    if (!firstLabel)
                        await writer.WriteAsync(',').ConfigureAwait(false);

                    firstLabel = false;

                    await writer.WriteAsync(label.Name).ConfigureAwait(false);
                    await writer.WriteAsync("=\"").ConfigureAwait(false);

                    // Have to escape the label values!
                    await writer.WriteAsync(EscapeLabelValue(label.Value)).ConfigureAwait(false);

                    await writer.WriteAsync('"').ConfigureAwait(false);
                }

                await writer.WriteAsync('}').ConfigureAwait(false);
            }

            await writer.WriteAsync(' ').ConfigureAwait(false);
            await writer.WriteLineAsync(value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
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
