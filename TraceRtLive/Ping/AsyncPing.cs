using System.Net;
using System.Net.NetworkInformation;
using NetPing = System.Net.NetworkInformation.Ping;

namespace TraceRtLive.Ping
{
    public sealed class AsyncPinger : IDisposable, IAsyncPing
    {
        public int TimeoutMilliseconds { get; }
        private NetPing Ping { get; }

        private long _payloadData;

        public AsyncPinger(int timeoutMilliseconds)
        {
            Ping = new NetPing();
            TimeoutMilliseconds = timeoutMilliseconds;
            _payloadData = DateTime.UtcNow.Ticks;
        }

        /// <inheritdoc/>
        public void Dispose() => Ping.Dispose();

        private byte[] NextPayload()
            => BitConverter.GetBytes(Interlocked.Increment(ref _payloadData));

        public async Task<PingReply> PingAsync(IPAddress target, int ttl, CancellationToken cancellation)
        {
            var result = await Ping.SendPingAsync(target, TimeoutMilliseconds, NextPayload(), new PingOptions { Ttl = ttl, DontFragment = true })
                .WaitAsync(cancellation) // allow cancellation
                .ConfigureAwait(false);
            return new PingReply
            {
                Address = result.Address,
                RoundtripTime = result.RoundtripTime,
                Status = result.Status,
            };
        }
    }
}
