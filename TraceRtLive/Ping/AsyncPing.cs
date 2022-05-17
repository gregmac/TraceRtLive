using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using TraceRtLive.Helpers;
using NetPing = System.Net.NetworkInformation.Ping;

namespace TraceRtLive.Ping
{
    /// <summary>
    /// Ping helper, allowing stat tracking and multiple pings.
    /// </summary>
    public sealed class AsyncPinger : IAsyncPing
    {
        /// <summary>
        /// Ping timeout, in milliseconds
        /// </summary>
        public int TimeoutMilliseconds { get; }

        /// <summary>
        /// Delay between multiple pings
        /// </summary>
        public int DelayBetweenMilliseconds { get; }

        /// <summary>
        /// Create a ping instance
        /// </summary>
        /// <param name="timeoutMilliseconds"><inheritdoc cref="TimeoutMilliseconds" path="/summary"/></param>
        /// <param name="delayBetweenMilliseconds"><inheritdoc cref="DelayBetweenMilliseconds" path="/summary"/></param>
        public AsyncPinger(int timeoutMilliseconds, int delayBetweenMilliseconds = 1000)
        {
            TimeoutMilliseconds = timeoutMilliseconds;
            DelayBetweenMilliseconds = delayBetweenMilliseconds;
        }

        /// <inheritdoc/>
        public async Task<PingReply> PingAsync(IPAddress target, int ttl = 128, CancellationToken cancellation = default)
            => await PingMultipleAsync(target, 1, ttl, cancellation).FirstAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public async IAsyncEnumerable<PingReply> PingMultipleAsync(IPAddress target, int numPings, int ttl = 128, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            using var ping = new NetPing();
            var stats = new TimeStats();

            var numSent = 0;
            var numFail = 0;

            var pingOptions = new PingOptions { Ttl = ttl, DontFragment = true };

            for (var i = 0; i < numPings; i++)
            {
                // wait except on first
                // do this first, so we don't have a wait at the end
                if (i > 0) await Task.Delay(DelayBetweenMilliseconds, cancellation).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();

                var sent = DateTime.UtcNow;
                var result = await ping.SendPingAsync(target, TimeoutMilliseconds, BitConverter.GetBytes(sent.Ticks), pingOptions)
                    .WaitAsync(cancellation) // allow cancellation
                    .ConfigureAwait(false);

                var rtt = DateTime.UtcNow.Subtract(sent);
                stats.Add(rtt);

                numSent++; 
                if (result.Status != IPStatus.Success && result.Status != IPStatus.TtlExpired) numFail++;

                yield return new PingReply
                {
                    Address = result.Address.Equals(IPAddress.Any) ? null : result.Address,
                    Status = result.Status,
                    RoundtripTime = rtt,
                    RoundTripTimeMin = stats.Min,
                    RoundTripTimeMax = stats.Max,
                    RoundTripTimeMean = stats.Mean,
                    NumSent = numSent,
                    NumFail = numFail,
                };

                cancellation.ThrowIfCancellationRequested();
            }
        }
    }
}
