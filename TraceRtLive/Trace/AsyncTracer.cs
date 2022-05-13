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
            var hops = 0;
            while (++hops <= maxHops)
            {
                var pingResult = await _ping.PingAsync(target, hops, CancellationToken.None);

                var result = new TraceResult
                {
                    Hops = hops,
                    IP = pingResult.Address,
                    RoundTripTime = pingResult.RoundtripTime,
                };


                if (pingResult.Status == IPStatus.Success)
                {
                    targetResultAction.Invoke(result);
                    return;
                }

                hopResultAction.Invoke(result);
                await Task.Yield();
            }
        }
    }
}
