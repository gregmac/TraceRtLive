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

        public async Task TraceAsync(IPAddress target, int maxHops, CancellationToken cancellation, Action<TraceResult> resultAction,
            int maxConcurrent = 5)
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
                    new ParallelOptions { MaxDegreeOfParallelism = numConcurrent, CancellationToken = cancellation },
                    async (hop, _) =>
                    {
                        // indicate started
                        resultAction.Invoke(new TraceResult
                        {
                            Status = TraceResultStatus.InProgress,
                            Hops = hop,
                        });

                        // execute ping
                        var pingResult = await _ping.PingAsync(target, hop, cancellation);

                        // check if target
                        if (target.Equals(pingResult.Address))
                        {
                            // always use lowest hop value
                            lock (targetMinHopsLock)
                            {
                                if (hop < targetMinHops) targetMinHops = hop;
                            }

                            // remove intermediate status result
                            resultAction.Invoke(new TraceResult
                            {
                                Status = TraceResultStatus.Obsolete,
                                Hops = hop,
                            });

                            // add final result
                            resultAction.Invoke(new TraceResult
                            {
                                Status = TraceResultStatus.FinalResult,
                                Hops = targetMinHops,
                                IP = pingResult.Address,
                                RoundTripTime = pingResult.RoundtripTime,
                            });
                        }
                        else
                        {
                            // add intermediate result
                            resultAction.Invoke(new TraceResult
                            {
                                Status = TraceResultStatus.HopResult,
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
                    });
            }

            await executeTrace(1);
        }
    }
}
