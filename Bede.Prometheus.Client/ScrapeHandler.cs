using System.Collections.Generic;
using System.IO;
using Prometheus.Internal;

namespace Prometheus
{
    public static class ScrapeHandler
    {
        public static readonly string ContentType = "text/plain; version=0.0.4";

        public static void ProcessScrapeRequest(
            IEnumerable<Advanced.DataContracts.MetricFamily> collected,
            Stream outputStream)
        {
            AsciiFormatter.Format(outputStream, collected);
        }
    }
}
