using System.Collections.Concurrent;
using System.Net;
using TraceRtLive.DNS;
using TraceRtLive.Helpers;
using TraceRtLive.Ping;

namespace TraceRtLive.Trace
{
    public sealed class AsyncTracer
    {
        private IAsyncPing _ping { get; }
        private IDnsResolver? _dnsResolver { get; }

        public AsyncTracer(int timeoutMilliseconds)
            : this(new AsyncPinger(timeoutMilliseconds), new DnsResolver()) { }

        public AsyncTracer(IAsyncPing ping, IDnsResolver? dnsResolver)
        {
            _ping = ping ?? throw new ArgumentNullException(nameof(ping));
            _dnsResolver = dnsResolver;
        }

        /// <summary>
        /// Run a trace. Optionally resolves reverse DNS entries and 
        /// </summary>
        /// <param name="target">Target IP</param>
        /// <param name="maxHops">Maximum number of hops to check.</param>
        /// <param name="cancellation">Cancels all tracing</param>
        /// <param name="resultAction">Callback for when an IP is resolved for number of
        /// hops. When the final target hop is found, this is called again with <see cref="TraceResultStatus.Obsolete"/>
        /// for that hop.</param>
        /// <param name="pingAction">Callback for when each ping is complete.
        /// If <see langword="null"/> only a single ping is sent to determine the IP and hop distance.</param>
        /// <param name="dnsResolvedAction">Callback for when DNS lookup is complete.
        /// If <see langword="null"/> no DNS lookups are performed.</param>
        /// <param name="maxConcurrent">Maximum number of concurrent hops to check</param>
        /// <param name="numPings">Number of pings to send to each hop</param>
        /// <returns></returns>
        public async Task TraceAsync(IPAddress target, int maxHops, CancellationToken cancellation,
            Action<int, TraceResultStatus, IPAddress?> resultAction,
            Action<int, PingReply>? pingAction = null,
            Action<int, IPHostEntry?>? dnsResolvedAction = null,
            int maxConcurrent = 5, int numPings = 5)
        {
            // record hops values
            var targetMinHops = int.MaxValue;
            var targetMinHopsLock = new object();

            // track ping tasks that remain
            var extraTasks = new ConcurrentBag<Task>();

            // trace function
            async Task executeTrace(int startHops)
            {
                // concurrent, unless we exceed maxHops
                var numConcurrent = startHops + maxConcurrent > maxHops
                    ? maxHops - startHops
                    : maxConcurrent;
                if (numConcurrent <= 0) return;


                await Parallel.ForEachAsync(
                    Enumerable.Range(startHops, numConcurrent),
                    new ParallelOptions { MaxDegreeOfParallelism = numConcurrent, CancellationToken = cancellation },
                    async (hops, _) =>
                    {
                        // indicate started
                        resultAction?.Invoke(hops, TraceResultStatus.InProgress, null);

                        // start ping execution
                        var pings = _ping.PingMultipleAsync(target, numPings: numPings, ttl: hops, cancellation: cancellation).GetAsyncEnumerator(cancellation);
                        var pingResult = await pings.NextOrDefaultAsync().ConfigureAwait(false);

                        // check if target
                        if (target.Equals(pingResult?.Address))
                        {
                            // always use lowest hop value
                            lock (targetMinHopsLock)
                            {
                                if (hops < targetMinHops) targetMinHops = hops;
                            }

                            // remove intermediate status result with wrong hops value
                            resultAction?.Invoke(hops, TraceResultStatus.Obsolete, null);

                            // add final result
                            resultAction?.Invoke(targetMinHops, TraceResultStatus.FinalResult, pingResult.Address);
                        }
                        else
                        {
                            resultAction?.Invoke(hops, TraceResultStatus.HopResult, pingResult?.Address);

                            // if last hop in batch, start the next batch
                            if (hops == startHops + numConcurrent - 1)
                            {
                                await executeTrace(startHops + numConcurrent);
                            }
                        }

                        if (dnsResolvedAction != null && _dnsResolver != null && pingResult?.Address?.IsValid() == true)
                        {
                            extraTasks.Add(Task.Run(async () =>
                            {
                                var result = await _dnsResolver.ResolveAsync(pingResult!.Address);
                                dnsResolvedAction.Invoke(hops, result);
                            }, cancellation));
                        }

                        if (pingAction != null)
                        {
                            // start background pings
                            extraTasks.Add(Task.Run(async () =>
                            {
                                while (pingResult != null)
                                {
                                    pingAction.Invoke(hops, pingResult);
                                    pingResult = await pings.NextOrDefaultAsync();
                                }
                            }, cancellation));
                        }
                    });
            }

            // execute trace starting from hop 1
            await executeTrace(1).ConfigureAwait(false);

            // wait for ping tasks to complete
            await Task.WhenAll(extraTasks).ConfigureAwait(false);
        }
    }
}
