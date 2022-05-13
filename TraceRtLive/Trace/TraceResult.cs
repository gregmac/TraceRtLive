using System.Net;

namespace TraceRtLive.Trace
{
    public class TraceResult
    {
        public int Hops { get; init; }
        public IPAddress? IP { get; init; }
        public long RoundTripTime { get; init; }
    }
}
