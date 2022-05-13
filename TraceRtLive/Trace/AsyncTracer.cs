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

        public async Task TraceAsync(IPAddress target, int maxHops, Action<TraceResult> hopResultAction, Action<TraceResult> targetResultAction,
            int maxConcurrent = 6)
        {

            // record 
            var targetMinHops = int.MaxValue;
            var targetMinHopsLock = new object();
            
            async Task executeTrace(int startHops)
            {
                // concurrent, unless we exceed maxHops
                var numConcurrent = startHops + maxConcurrent > maxHops
                    ? maxHops - startHops
                    : maxConcurrent;
                if (numConcurrent <= 0) return;

                await Parallel.ForEachAsync(
                    Enumerable.Range(startHops, numConcurrent),
                    new ParallelOptions { MaxDegreeOfParallelism = numConcurrent, },
                    async (hop, _) =>
                    {
                        try
                        {
                            // indicate started
                            hopResultAction.Invoke(new TraceResult
                            {
                                Hops = hop,
                                InProgress = true,
                            });

                            // execute ping
                            var pingResult = await _ping.PingAsync(target, hop, CancellationToken.None);

                            // check if target
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
                            }
                            else
                            {
                                hopResultAction.Invoke(new TraceResult
                                {
                                    Hops = hop,
                                    IP = pingResult.Address,
                                    RoundTripTime = pingResult.RoundtripTime,
                                });

                                // if last hop in batch, start the next batch
                                if (hop == startHops + numConcurrent - 1)
                                {
                                    await executeTrace(startHops + numConcurrent);
                                }
                            }
                        }
                        catch (TaskCanceledException) { /* ignore */ }
                    });
            }

            await executeTrace(1);
        }
    }
}
