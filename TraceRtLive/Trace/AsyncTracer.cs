using System.Collections.Concurrent;
using System.Net;
using TraceRtLive.Helpers;
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

        public async Task TraceAsync(IPAddress target, int maxHops, CancellationToken cancellation,
            Action<int, TraceResultStatus, IPAddress?> resultAction,
            Action<int, PingReply> pingAction,
            int maxConcurrent = 5, int numPings = 5)
        {
            // record hops values
            var targetMinHops = int.MaxValue;
            var targetMinHopsLock = new object();

            // track ping tasks that remain
            var pingTasks = new ConcurrentBag<Task>();

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

                        // start background pings
                        pingTasks.Add(Task.Run(async () =>
                        {
                            while (pingResult != null)
                            {
                                pingAction?.Invoke(hops, pingResult);
                                pingResult = await pings.NextOrDefaultAsync();
                            }
                        }));
                    });
            }

            // execute trace starting from hop 1
            await executeTrace(1).ConfigureAwait(false);

            // wait for ping tasks to complete
            await Task.WhenAll(pingTasks).ConfigureAwait(false);
        }
    }
}
