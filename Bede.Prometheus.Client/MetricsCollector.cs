using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Prometheus.Advanced;
using Prometheus.Internal;

namespace Prometheus
{
    public interface IMetricsCollector
    {
        string ContentType { get; }

        StreamWriter CreateWriter(Stream stream);

        Task WriteAsync(StreamWriter writer);
    }

    public class MetricsCollector : IMetricsCollector
    {
        private static readonly Encoding _encoding = new UTF8Encoding(false);
        private readonly ICollectorRegistry _registry;

        public MetricsCollector(ICollectorRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public string ContentType { get; } = "text/plain; version=0.0.4";

        public StreamWriter CreateWriter(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return new StreamWriter(stream, _encoding, bufferSize: 1024, leaveOpen: true);
        }

        public async Task WriteAsync(StreamWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var collected = _registry.CollectAll();

            await Serializer.SerializeAsync(writer, collected).ConfigureAwait(false);
        }
    }
}
