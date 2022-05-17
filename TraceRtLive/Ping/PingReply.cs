using System.Net;
using System.Net.NetworkInformation;

namespace TraceRtLive.Ping
{
    public class PingReply
    {
        public IPStatus Status { get; init; }

        public IPAddress? Address { get; init; }

        public TimeSpan RoundtripTime { get; init; }
        public TimeSpan RoundTripTimeMin { get; init; }
        public TimeSpan RoundTripTimeMax { get; init; }
        public TimeSpan RoundTripTimeMean { get; init; }
        public int NumSent { get; init; }
        public int NumFail { get; init; }
    }
}
