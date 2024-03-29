﻿using System.Net;

namespace TraceRtLive.Ping
{
    public interface IAsyncPing
    {
        /// <summary>
        /// Execute a single ICMP Echo Requests to the given <paramref name="target"/>.
        /// </summary>
        /// <inheritdoc cref="PingMultipleAsync(IPAddress, int, int, CancellationToken)"/>
        Task<PingReply> PingAsync(IPAddress target, int ttl = 128, CancellationToken cancellation = default);

        /// <summary>
        /// Execute multiple ICMP Echo Requests to the given <paramref name="target"/>.
        /// </summary>
        /// <remarks>
        /// The payload is 4 bytes based on current UTC time.
        /// </remarks>
        /// <param name="target">IP to ping</param>
        /// <param name="numPings">How many pings should be sent</param>
        /// <param name="ttl">Time To Live: number of routing hops allowed before the ping is discarded. Defaults to 128.</param>
        /// <param name="numHistoryPings">How many pings to track in <see cref="PingReply.History"/>.</param>
        /// <param name="cancellation">Cancel the ping operation</param>
        IAsyncEnumerable<PingReply> PingMultipleAsync(IPAddress target, int numPings, int ttl = 128, int numHistoryPings = 0, CancellationToken cancellation = default);
    }
}
