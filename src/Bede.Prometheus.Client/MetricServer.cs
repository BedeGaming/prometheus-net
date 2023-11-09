using Prometheus.Advanced;
using Prometheus.Advanced.DataContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus
{
    /// <summary>
    /// Implementation of a Prometheus exporter that serves metrics using HttpListener.
    /// </summary>
    public class MetricServer : MetricHandler
    {
        private readonly HttpListener _httpListener = new HttpListener();

        public MetricServer(int port, string url = "metrics/", ICollectorRegistry registry = null, bool useHttps = false) : this("+", port, url, registry, useHttps)
        {
        }

        public MetricServer(string hostname, int port, string url = "metrics/", ICollectorRegistry registry = null, bool useHttps = false) : base(registry)
        {
            var s = useHttps ? "s" : "";
            _httpListener.Prefixes.Add($"http{s}://{hostname}:{port}/{url}");
        }

        protected override Task StartServer(CancellationToken cancel)
        {
            // This will ensure that any failures to start are nicely thrown from StartServerAsync.
            _httpListener.Start();

            // Kick off the actual processing to a new thread and return a Task for the processing thread.
            return Task.Factory.StartNew(async delegate
            {
                try
                {
                    while (!cancel.IsCancellationRequested)
                    {
                        // There is no way to give a CancellationToken to GCA() so, we need to hack around it a bit.
                        var getContext = _httpListener.GetContextAsync();
                        getContext.Wait(cancel);
                        var context = getContext.Result;
                        var request = context.Request;
                        var response = context.Response;

                        response.StatusCode = 200;
                        response.ContentType = _collector.ContentType;

                        try
                        {
                            try
                            {
                                using (var writer = _collector.CreateWriter(response.OutputStream))
                                {
                                    await _collector.WriteAsync(writer).ConfigureAwait(false);
                                }
                            }
                            catch (ScrapeFailedException)
                            {
                                response.StatusCode = 503;

                                continue;
                            }
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            Trace.WriteLine(string.Format("Error in MetricsServer: {0}", ex));

                            try
                            {
                                response.StatusCode = 500;
                            }
                            catch
                            {
                                // Might be too late in request processing to set response code, so just ignore.
                            }
                        }
                        finally
                        {
                            response.Close();
                        }
                    }
                }
                finally
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
