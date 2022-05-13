using System.Net;
using System.Net.NetworkInformation;
using NetPing = System.Net.NetworkInformation.Ping;

namespace TraceRtLive.Ping
{
    public sealed class AsyncPinger : IAsyncPing
    {
        public int TimeoutMilliseconds { get; }

        public AsyncPinger(int timeoutMilliseconds)
        {
            TimeoutMilliseconds = timeoutMilliseconds;
        }

        public async Task<PingReply> PingAsync(IPAddress target, int ttl, CancellationToken cancellation)
        {
            using var ping = new NetPing();

            var pingOptions = new PingOptions { Ttl = ttl, DontFragment = true };
            var sent = DateTime.UtcNow;
            var result = await ping.SendPingAsync(target, TimeoutMilliseconds, BitConverter.GetBytes(sent.Ticks), pingOptions)
                .WaitAsync(cancellation) // allow cancellation
                .ConfigureAwait(false);
            return new PingReply
            {
                Address = result.Address,
                RoundtripTime = DateTime.UtcNow.Subtract(sent),
                Status = result.Status,
            };
        }
    }
}
