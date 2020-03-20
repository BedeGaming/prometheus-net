using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Prometheus.Internal;

namespace Prometheus
{
    public static class ScrapeHandler
    {
        public static readonly string ContentType = "text/plain; version=0.0.4";

        public static async Task ProcessScrapeRequest(
            IEnumerable<Advanced.DataContracts.MetricFamily> collected,
            Stream outputStream)
        {
            await AsciiFormatter.FormatAsync(outputStream, collected).ConfigureAwait(false);
        }
    }
}
