using System.Net;
using System.Net.NetworkInformation;
using NetPing = System.Net.NetworkInformation.Ping;

namespace TraceRtLive.Ping
{
    public sealed class AsyncPinger : IDisposable, IAsyncPing
    {
        public int TimeoutMilliseconds { get; }
        private NetPing Ping { get; }

        public AsyncPinger(int timeoutMilliseconds)
        {
            Ping = new NetPing();
            TimeoutMilliseconds = timeoutMilliseconds;
        }

        /// <inheritdoc/>
        public void Dispose() => Ping.Dispose();

        public async Task<PingReply> PingAsync(IPAddress target, int ttl, CancellationToken cancellation)
        {
            var pingOptions = new PingOptions { Ttl = ttl, DontFragment = true };
            var sent = DateTime.UtcNow;
            var result = await Ping.SendPingAsync(target, TimeoutMilliseconds, BitConverter.GetBytes(sent.Ticks), pingOptions)
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
