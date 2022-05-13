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

            var hops = 1;
            while (hops <= maxHops && !cancellation.IsCancellationRequested)
            {
                var numHops = 5;

                await Parallel.ForEachAsync(
                    Enumerable.Range(hops, numHops),
                    //new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = cancellation.Token },
                    async (hop,_) =>
                    {
                        try
                        {
                            var pingResult = await _ping.PingAsync(target, hop, cancellation.Token);

                            var result = new TraceResult
                            {
                                Hops = hop,
                                IP = pingResult.Address,
                                RoundTripTime = pingResult.RoundtripTime,
                            };

                            if (pingResult.Status == IPStatus.Success)
                            {
                                targetResultAction.Invoke(result);

                                // cancel others
                                cancellation.Cancel();
                            }
                            else
                            {
                                hopResultAction.Invoke(result);
                            }
                        }
                        catch(TaskCanceledException) { /* ignore */ }

                        await Task.Yield();
                    });

                hops += numHops;
            }
        }
    }
}
