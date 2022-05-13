using System.Net;
using System.Net.NetworkInformation;

namespace TraceRtLive.Ping
{
    public interface IAsyncPing
    {
        Task<PingReply> PingAsync(IPAddress target, int ttl, CancellationToken cancellation);
    }
}
