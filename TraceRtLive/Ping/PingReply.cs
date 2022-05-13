using System.Net;
using System.Net.NetworkInformation;

namespace TraceRtLive.Ping
{
    public class PingReply
    {
        public IPStatus Status { get; init; }

        public IPAddress? Address { get; init; }

        public TimeSpan RoundtripTime { get; init; }

    }
}
