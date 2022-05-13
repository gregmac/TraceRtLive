using System.Net;
using System.Net.NetworkInformation;
using TraceRtLive.Ping;

namespace TraceRtLive.Trace
{
    public sealed class AsyncTracer
    {
        private IAsyncPing _ping { get; }

        public AsyncTracer(int timeoutMilliseconds)
            : this(new AsyncPinger(timeoutMilliseconds)) { }

        public AsyncTracer(IAsyncPing ping)
        {
            _ping = ping;
        }

        public async Task TraceAsync(IPAddress target, int maxHops, Action<TraceResult> hopResultAction, Action<TraceResult> targetResultAction)
        {
            var cancellation = new CancellationTokenSource();

            // record 
            var targetMinHops = int.MaxValue;
            var targetMinHopsLock = new object();

            var hops = 1;
            while (hops <= maxHops && !cancellation.IsCancellationRequested)
            {
                var numConcurrent = 5;

                await Parallel.ForEachAsync(
                    Enumerable.Range(hops, numConcurrent),
                    new ParallelOptions { MaxDegreeOfParallelism = numConcurrent, },
                    async (hop,_) =>
                    {
                        try
                        {
                            var pingResult = await _ping.PingAsync(target, hop, cancellation.Token);

                            if (target.Equals(pingResult.Address))
                            {
                                // always use lowest hop value
                                lock (targetMinHopsLock)
                                {
                                    if (hop < targetMinHops) targetMinHops = hop;
                                }

                                targetResultAction.Invoke(new TraceResult
                                {
                                    Hops = targetMinHops,
                                    IP = pingResult.Address,
                                    RoundTripTime = pingResult.RoundtripTime,
                                });

                                // cancel others
                                cancellation.Cancel();
                            }
                            else
                            {
                                hopResultAction.Invoke(new TraceResult
                                {
                                    Hops = hop,
                                    IP = pingResult.Address,
                                    RoundTripTime = pingResult.RoundtripTime,
                                });
                            }
                        }
                        catch(TaskCanceledException) { /* ignore */ }

                        await Task.Yield();
                    });

                hops += numConcurrent;
            }
        }
    }
}
